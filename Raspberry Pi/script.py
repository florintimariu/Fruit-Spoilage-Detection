#!/usr/bin/env python3
import json
import os
import time
import threading
import requests
import serial
import pynmea2
import cv2
import torch
import numpy as np
import paho.mqtt.client as mqtt
from datetime import datetime, timezone
from collections import defaultdict
from segment_anything import sam_model_registry
from torchvision.transforms import functional as F
from torchvision.models.detection import maskrcnn_resnet50_fpn

# ============================================================
# CONFIG GENERAL
# ============================================================
OFFLINE_MODE = False  # True = salveaza tot local, False = trimite la backend

BACKEND_URL = "http://100.92.208.21:5056"
FIREBASE_API_KEY = "AIzaSyCO6pA-_x6GuG8FHQATubmIC_AO0qwxaRk"
RPI_EMAIL = "rpi-device@freshledger.com"
RPI_PASSWORD = "securepassword"

SHIPMENT_ID = "5a4f47cd-8e75-4c54-966f-c17b93983c17"

MQTT_BROKER = "localhost"
MQTT_PORT = 1883
MQTT_TOPIC = "zigbee2mqtt/+"

GPS_PORT = "/dev/serial0"
GPS_BAUD = 9600

BATCH_INTERVAL = 60          # secunde intre trimiterile de citiri senzori
INSPECTION_INTERVAL = 180   # secunde intre inspectiile AI (30 min)

LOCAL_CACHE_FILE = "pending_batches.json"

CAMERA_URL = "http://100.66.79.94:8080/video"  # URL camera IP pe WiFi

SPOILAGE_MODEL_PATH = "best_apple_spoilage_sam.pth"
CAPTURE_PATH = "capture.jpg"
 
 
# ============================================================
# CONFIG MODEL AI
# ============================================================
class AiConfig:
    DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
 
    # Stage 1 - Mask R-CNN
    DETECTION_CONFIDENCE_THRESHOLD = 0.5
    FRUIT_CLASS_IDS = {52: "banana", 53: "apple", 55: "orange"}
    CROP_PADDING = 20
 
    # Stage 2 - SAM + SpoilageHead
    MODEL_TYPE = "vit_b"
    IMG_SIZE = 1024
    NUM_CLASSES = 3
    MIN_FRUIT_SIZE = 500
 
 
ai_config = AiConfig()
 
 
# ============================================================
# STATE GLOBAL
# ============================================================
readings_buffer = defaultdict(list)
buffer_lock = threading.Lock()
latest_gps = {"lat": None, "lon": None, "speed": None}
gps_lock = threading.Lock()
 
id_token = None
token_expiry = 0
 
assigned_sensors = set()  # ieee-uri deja asignate shipment-ului curent
assigned_sensors_lock = threading.Lock()
 
 
# ============================================================
# MODEL ARHITECTURA (Stage 2)
# ============================================================
class SpoilageHead(torch.nn.Module):
    def __init__(self, input_dim=256, num_classes=3):
        super().__init__()
        self.conv1 = torch.nn.Conv2d(input_dim, 128, kernel_size=3, padding=1)
        self.bn1 = torch.nn.BatchNorm2d(128)
        self.conv2 = torch.nn.Conv2d(128, 64, kernel_size=3, padding=1)
        self.bn2 = torch.nn.BatchNorm2d(64)
        self.conv3 = torch.nn.Conv2d(64, num_classes, kernel_size=1)
        self.relu = torch.nn.ReLU()
 
    def forward(self, x):
        x = self.relu(self.bn1(self.conv1(x)))
        x = self.relu(self.bn2(self.conv2(x)))
        x = self.conv3(x)
        x = torch.nn.functional.interpolate(
            x, size=(ai_config.IMG_SIZE, ai_config.IMG_SIZE),
            mode='bilinear', align_corners=True
        )
        return x
 
 
class FineTunedSAM(torch.nn.Module):
    def __init__(self, sam_model, num_classes=3):
        super().__init__()
        self.sam = sam_model
        self.spoilage_head = SpoilageHead(num_classes=num_classes)
 
    def forward(self, x):
        with torch.no_grad():
            image_embeddings = self.sam.image_encoder(x)
        output = self.spoilage_head(image_embeddings)
        return output
 
 
# ============================================================
# STAGE 1 — MASK R-CNN
# ============================================================
def load_maskrcnn():
    print("[AI] Loading Mask R-CNN (ResNet-50 + FPN, COCO pretrained)...")
    model = maskrcnn_resnet50_fpn(weights="DEFAULT")
    model.to(ai_config.DEVICE)
    model.eval()
    return model
 
 
def detect_and_crop_fruit(maskrcnn_model, image_rgb):
    image_tensor = F.to_tensor(image_rgb).to(ai_config.DEVICE)
 
    with torch.no_grad():
        predictions = maskrcnn_model([image_tensor])[0]
 
    boxes = predictions["boxes"].cpu().numpy()
    labels = predictions["labels"].cpu().numpy()
    scores = predictions["scores"].cpu().numpy()
 
    h_img, w_img = image_rgb.shape[:2]
    best = None
 
    for box, label, score in zip(boxes, labels, scores):
        if score < ai_config.DETECTION_CONFIDENCE_THRESHOLD:
            continue
        if label not in ai_config.FRUIT_CLASS_IDS:
            continue
        if best is None or score > best["score"]:
            best = {"box": box, "label": ai_config.FRUIT_CLASS_IDS[label], "score": float(score)}
 
    if best is None:
        print("[AI] No fruit detected by Mask R-CNN")
        return None
 
    x1, y1, x2, y2 = best["box"].astype(int)
    pad = ai_config.CROP_PADDING
    x1 = max(0, x1 - pad)
    y1 = max(0, y1 - pad)
    x2 = min(w_img, x2 + pad)
    y2 = min(h_img, y2 + pad)
 
    crop = image_rgb[y1:y2, x1:x2].copy()
    print(f"[AI] Detected {best['label']} (score={best['score']:.2f})")
 
    return {"crop": crop, "bbox": (x1, y1, x2, y2), "label": best["label"], "score": best["score"]}
 
 
# ============================================================
# STAGE 2 — SAM + SPOILAGEHEAD
# ============================================================
def load_spoilage_model(model_path):
    if not os.path.exists(model_path):
        print(f"[AI] Error: model not found at {model_path}")
        return None
 
    sam = sam_model_registry[ai_config.MODEL_TYPE](checkpoint=None)
    model = FineTunedSAM(sam, num_classes=ai_config.NUM_CLASSES)
 
    try:
        checkpoint = torch.load(model_path, map_location=ai_config.DEVICE, weights_only=True)
    except Exception:
        checkpoint = torch.load(model_path, map_location=ai_config.DEVICE)
 
    if isinstance(checkpoint, dict):
        state_dict = checkpoint.get("model_state_dict", checkpoint.get("state_dict", checkpoint))
    else:
        state_dict = checkpoint
 
    model.load_state_dict(state_dict)
    model.to(ai_config.DEVICE)
    model.eval()
    print("[AI] Spoilage model loaded successfully")
    return model
 
 
def clean_mask(mask, min_size=None):
    if min_size is None:
        min_size = ai_config.MIN_FRUIT_SIZE
 
    mask_uint8 = mask.astype(np.uint8) * 255
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    mask_clean = cv2.morphologyEx(mask_uint8, cv2.MORPH_OPEN, kernel)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7))
    mask_clean = cv2.morphologyEx(mask_clean, cv2.MORPH_CLOSE, kernel)
 
    num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(mask_clean)
    mask_filtered = np.zeros_like(mask_clean)
    for i in range(1, num_labels):
        if stats[i, cv2.CC_STAT_AREA] >= min_size:
            mask_filtered[labels == i] = 255
 
    return mask_filtered > 0
 
 
def segment_spoilage(spoilage_model, crop_rgb, morphology_enabled=True):
    original_shape = crop_rgb.shape
    resized = cv2.resize(crop_rgb, (ai_config.IMG_SIZE, ai_config.IMG_SIZE))
    image_tensor = F.to_tensor(resized).unsqueeze(0).to(ai_config.DEVICE)
 
    with torch.no_grad():
        output = spoilage_model(image_tensor)
 
    probs = torch.softmax(output, dim=1)
    preds = torch.argmax(probs, dim=1)
    mask = preds.squeeze().cpu().numpy()
    probs_np = probs.squeeze().cpu().numpy()
 
    mask_resized = cv2.resize(
        mask.astype(np.uint8),
        (original_shape[1], original_shape[0]),
        interpolation=cv2.INTER_NEAREST
    )
 
    fruit_mask = (mask_resized == 1) | (mask_resized == 2)
    if morphology_enabled:
        fruit_mask = clean_mask(fruit_mask)
 
    probs_resized = np.zeros((ai_config.NUM_CLASSES, mask_resized.shape[0], mask_resized.shape[1]))
    for c in range(ai_config.NUM_CLASSES):
        probs_resized[c] = cv2.resize(probs_np[c], (mask_resized.shape[1], mask_resized.shape[0]))
 
    return mask_resized, fruit_mask, probs_resized
 
 
# ============================================================
# STAGE 3 — VIZUALIZARI + ANALIZA
# ============================================================
def create_segmented_fruit(image, mask):
    white_bg = np.ones_like(image) * 255
    segmented = white_bg.copy()
    segmented[mask] = image[mask]
    return segmented
 
 
def create_spoilage_overlay(image, mask):
    overlay = image.copy()
 
    fresh_mask = mask == 2
    if np.any(fresh_mask):
        overlay[fresh_mask] = (image[fresh_mask] * 0.7 + np.array([255, 0, 0]) * 0.3).astype(np.uint8)
 
    spoiled_mask = mask == 1
    if np.any(spoiled_mask):
        overlay[spoiled_mask] = (image[spoiled_mask] * 0.7 + np.array([0, 255, 0]) * 0.3).astype(np.uint8)
 
    return overlay
 
 
def analyze_segmentation(mask, probs):
    total_pixels = mask.size
    fresh_pixels = np.sum(mask == 2)
    spoiled_pixels = np.sum(mask == 1)
    apple_pixels = fresh_pixels + spoiled_pixels
 
    if apple_pixels == 0:
        return {
            'apple_detected': False, 'fresh_percentage': 0, 'spoiled_percentage': 0,
            'confidence': 0, 'status': 'No apple detected', 'apple_coverage': 0
        }
 
    fresh_pct = (fresh_pixels / apple_pixels) * 100
    spoiled_pct = (spoiled_pixels / apple_pixels) * 100
 
    apple_mask = mask > 0
    avg_confidence = np.mean(np.max(probs, axis=0)[apple_mask]) if np.any(apple_mask) else 0
 
    if spoiled_pct > 20:
        status = 'Heavily Spoiled'
    elif spoiled_pct > 5:
        status = 'Partially Spoiled'
    else:
        status = 'Fresh'
 
    return {
        'apple_detected': True, 'fresh_percentage': fresh_pct, 'spoiled_percentage': spoiled_pct,
        'confidence': avg_confidence, 'status': status,
        'apple_coverage': (apple_pixels / total_pixels) * 100
    }
 
 
def run_ai_pipeline(maskrcnn_model, spoilage_model, image_path):
    image_bgr = cv2.imread(image_path)
    if image_bgr is None:
        print(f"[AI] Could not read image: {image_path}")
        return None
    image_rgb = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2RGB)
 
    detection = detect_and_crop_fruit(maskrcnn_model, image_rgb)
    if detection is None:
        return None
 
    crop = detection["crop"]
    mask_resized, fruit_mask, probs_resized = segment_spoilage(spoilage_model, crop)
 
    segmented_fruit = create_segmented_fruit(crop, fruit_mask)
    spoilage_overlay = create_spoilage_overlay(crop, mask_resized)
    analysis = analyze_segmentation(mask_resized, probs_resized)
 
    base = os.path.splitext(os.path.basename(image_path))[0]
    segmented_path = f"{base}_fruit_segmented.jpg"
    spoilage_path = f"{base}_fruit_with_mask.jpg"
 
    cv2.imwrite(segmented_path, cv2.cvtColor(segmented_fruit, cv2.COLOR_RGB2BGR))
    cv2.imwrite(spoilage_path, cv2.cvtColor(spoilage_overlay, cv2.COLOR_RGB2BGR))
 
    return {
        "analysis": analysis,
        "segmented_path": segmented_path,
        "spoilage_path": spoilage_path,
        "detection": detection,
    }
 
 
# ============================================================
# CAPTURA IMAGINE DE LA CAMERA IP (WiFi)
# ============================================================
def capture_frame(camera_url, output_path):
    cap = cv2.VideoCapture(camera_url)
    if not cap.isOpened():
        print("[CAMERA] Nu ma pot conecta la camera")
        return False
 
    ret, frame = cap.read()
    cap.release()
 
    if not ret or frame is None:
        print("[CAMERA] Frame invalid")
        return False
 
    cv2.imwrite(output_path, frame)
    print(f"[CAMERA] Frame capturat: {output_path}")
    return True
 
 
# ============================================================
# AUTENTIFICARE FIREBASE
# ============================================================
def get_id_token():
    global id_token, token_expiry
 
    if OFFLINE_MODE:
        id_token = "offline"
        token_expiry = time.time() + 86400
        return id_token
 
    url = f"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FIREBASE_API_KEY}"
    resp = requests.post(url, json={
        "email": RPI_EMAIL,
        "password": RPI_PASSWORD,
        "returnSecureToken": True
    })
    resp.raise_for_status()
    data = resp.json()
    id_token = data["idToken"]
    token_expiry = time.time() + int(data["expiresIn"]) - 600
    print(f"[AUTH] Token obtinut, expira in {data['expiresIn']}s")
    return id_token
 
 
def ensure_token():
    if id_token is None or time.time() > token_expiry:
        get_id_token()
    return id_token
 
 
# ============================================================
# CACHE LOCAL (batch-uri de senzori)
# ============================================================
def save_batch_locally(batch):
    existing = []
    if os.path.exists(LOCAL_CACHE_FILE):
        with open(LOCAL_CACHE_FILE, "r") as f:
            try:
                existing = json.load(f)
            except Exception:
                existing = []
    existing.append(batch)
    with open(LOCAL_CACHE_FILE, "w") as f:
        json.dump(existing, f, indent=2)
    print(f"[CACHE] Salvat local ({len(existing)} batches total)")
 
 
def load_pending_batches():
    if not os.path.exists(LOCAL_CACHE_FILE):
        return []
    with open(LOCAL_CACHE_FILE, "r") as f:
        try:
            return json.load(f)
        except Exception:
            return []
 
 
def clear_local_cache():
    if os.path.exists(LOCAL_CACHE_FILE):
        os.remove(LOCAL_CACHE_FILE)
        print("[CACHE] Cache local sters")
 
 
def get_active_step():
    if OFFLINE_MODE:
        return "offline-step"
 
    try:
        token = ensure_token()
        url = f"{BACKEND_URL}/api/shipments/{SHIPMENT_ID}/steps"
        resp = requests.get(url, headers={"Authorization": f"Bearer {token}"}, timeout=10)
 
        if resp.status_code != 200:
            print(f"[STEP] Eroare query steps: {resp.status_code}")
            return None
 
        steps = resp.json()
        active = [s for s in steps if not s.get("isCompleted", True)]
        if not active:
            print("[STEP] Niciun step activ")
            return None
        return active[0]["stepId"]
 
    except Exception as e:
        print(f"[STEP] Backend indisponibil: {e}")
        return None
 
 
def assign_sensor_to_shipment(ieee):
    """Asigneaza senzorul (dupa IEEE) la SHIPMENT_ID curent, o singura data per rulare."""
    if OFFLINE_MODE:
        return
 
    with assigned_sensors_lock:
        if ieee in assigned_sensors:
            return
 
    try:
        token = ensure_token()
        url = f"{BACKEND_URL}/api/sensors/{ieee}/assign"
        resp = requests.post(
            url,
            headers={"Authorization": f"Bearer {token}"},
            json={"shipmentId": SHIPMENT_ID},
            timeout=10
        )
        if resp.status_code == 200:
            with assigned_sensors_lock:
                assigned_sensors.add(ieee)
            print(f"[ASSIGN] Senzor {ieee} asignat la shipment {SHIPMENT_ID}")
        else:
            print(f"[ASSIGN] Eroare {resp.status_code} pentru {ieee}: {resp.text}")
    except Exception as e:
        print(f"[ASSIGN] Eroare conexiune pentru {ieee}: {e}")
 
 
def upload_pending_batches():
    if OFFLINE_MODE:
        return
 
    batches = load_pending_batches()
    if not batches:
        return
 
    print(f"[RETRY] {len(batches)} batches pending, incerc upload...")
    step_id = get_active_step()
    if step_id is None:
        print("[RETRY] Niciun step activ, skip retry")
        return
 
    token = ensure_token()
    uploaded = 0
 
    for batch in batches:
        url = f"{BACKEND_URL}/api/shipments/{SHIPMENT_ID}/steps/{step_id}/readings/batch"
        try:
            resp = requests.post(url, headers={"Authorization": f"Bearer {token}"},
                                  json=batch, timeout=10)
            if resp.status_code == 200:
                uploaded += 1
            else:
                print(f"[RETRY] Eroare {resp.status_code}: {resp.text}")
                break
        except Exception as e:
            print(f"[RETRY] Eroare conexiune: {e}")
            break
 
    if uploaded == len(batches):
        clear_local_cache()
        print(f"[RETRY] Toate {uploaded} batches uploadate cu succes")
    else:
        remaining = batches[uploaded:]
        with open(LOCAL_CACHE_FILE, "w") as f:
            json.dump(remaining, f, indent=2)
        print(f"[RETRY] {uploaded}/{len(batches)} uploadate, {len(remaining)} ramase")
 
 
# ============================================================
# MQTT — COLECTARE CITIRI ZIGBEE
# ============================================================
def on_connect(client, userdata, flags, rc):
    print(f"[MQTT] Conectat (rc={rc})")
    client.subscribe(MQTT_TOPIC)
 
 
def on_message(client, userdata, msg):
    try:
        topic_parts = msg.topic.split("/")
        if len(topic_parts) < 2:
            return
 
        ieee = topic_parts[1]
        if ieee in ("bridge",) or ieee.startswith("bridge"):
            return
 
        # Asigneaza senzorul la shipment-ul curent (o singura data per ieee, non-blocant)
        with assigned_sensors_lock:
            already_assigned = ieee in assigned_sensors
        if not already_assigned:
            threading.Thread(target=assign_sensor_to_shipment, args=(ieee,), daemon=True).start()
 
        payload = json.loads(msg.payload.decode())
        extracted = {}
 
        if "temperature" in payload:
            extracted["temperature"] = payload["temperature"]
        if "humidity" in payload:
            extracted["humidity"] = payload["humidity"]
 
        for key in ("analog_input_1", "analog_input_2", "analog_input_3",
                    "analog_input_4", "analog_input_5", "analog_input_6"):
            if key in payload:
                extracted[key] = payload[key]
 
        if extracted:
            with buffer_lock:
                for key, value in extracted.items():
                    readings_buffer[f"{ieee}_{key}"].append(value)
            print(f"[MQTT] {ieee}: {extracted}")
 
    except Exception as e:
        print(f"[MQTT] Eroare procesare mesaj: {e}")
 
 
# ============================================================
# GPS — CITIRE PE UART (NEO-6M, NMEA)
# ============================================================
def gps_reader():
    try:
        ser = serial.Serial(GPS_PORT, GPS_BAUD, timeout=1)
        print(f"[GPS] Port deschis: {GPS_PORT} @ {GPS_BAUD} baud")
    except Exception as e:
        print(f"[GPS] Nu pot deschide portul: {e}")
        return
 
    while True:
        try:
            line = ser.readline().decode("ascii", errors="replace").strip()
            if line.startswith("$GPRMC") or line.startswith("$GPGGA"):
                msg = pynmea2.parse(line)
                if hasattr(msg, "latitude") and msg.latitude != 0:
                    with gps_lock:
                        latest_gps["lat"] = msg.latitude
                        latest_gps["lon"] = msg.longitude
                        if hasattr(msg, "spd_over_grnd") and msg.spd_over_grnd:
                            latest_gps["speed"] = float(msg.spd_over_grnd) * 0.514  # knots -> m/s
        except Exception:
            pass
 
 
# ============================================================
# BATCH SENDER — CITIRI SENZORI + GPS (thread separat)
# ============================================================
def batch_sender():
    while True:
        time.sleep(BATCH_INTERVAL)
 
        with buffer_lock:
            current = dict(readings_buffer)
            readings_buffer.clear()
 
        if not current:
            print("[BATCH] Nimic de trimis")
            continue
 
        now = datetime.now(timezone.utc).isoformat()
        readings = []
        for key, values in current.items():
            idx = key.find("_analog_input_")
            if idx != -1:
                ieee = key[:idx]
                sensor_logical_id = key[idx + 1:]
            elif "_temperature" in key:
                idx = key.find("_temperature")
                ieee = key[:idx]
                sensor_logical_id = "temperature"
            elif "_humidity" in key:
                idx = key.find("_humidity")
                ieee = key[:idx]
                sensor_logical_id = "humidity"
            else:
                ieee = key
                sensor_logical_id = "unknown"
 
            avg_value = sum(values) / len(values)
            readings.append({
                "sensorIeee": ieee,
                "sensorLogicalId": sensor_logical_id,
                "value": round(avg_value, 2),
                "timestamp": now
            })
 
        location = None
        with gps_lock:
            if latest_gps["lat"] is not None:
                location = {
                    "latitude": latest_gps["lat"],
                    "longitude": latest_gps["lon"],
                    "accuracy": None,
                    "speed": latest_gps["speed"],
                    "timestamp": now
                }
 
        batch_payload = {"readings": readings, "location": location}
 
        if OFFLINE_MODE:
            save_batch_locally(batch_payload)
            print(f"[BATCH] Salvat local: {len(readings)} readings")
            continue
 
        step_id = get_active_step()
        if step_id is None:
            print("[BATCH] Niciun step activ, salvez local...")
            save_batch_locally(batch_payload)
            continue
 
        token = ensure_token()
        url = f"{BACKEND_URL}/api/shipments/{SHIPMENT_ID}/steps/{step_id}/readings/batch"
        try:
            resp = requests.post(url, headers={"Authorization": f"Bearer {token}"},
                                  json=batch_payload, timeout=10)
            if resp.status_code == 200:
                result = resp.json()
                print(f"[BATCH] Trimis: {result['readingsAccepted']} readings, "
                      f"GPS: {result['locationAccepted']}")
                upload_pending_batches()
            else:
                print(f"[BATCH] Eroare {resp.status_code}, salvez local...")
                save_batch_locally(batch_payload)
        except Exception as e:
            print(f"[BATCH] Fara conexiune ({e}), salvez local...")
            save_batch_locally(batch_payload)
 
 
# ============================================================
# UPLOAD IMAGINI PE CATBOX
# ============================================================
def upload_to_catbox(local_path):
    with open(local_path, "rb") as f:
        resp = requests.post(
            "https://catbox.moe/user/api.php",
            data={"reqtype": "fileupload"},
            files={"fileToUpload": f}
        )
    resp.raise_for_status()
    return resp.text.strip()
 
 
def send_inspection(token, step_id, image_url, mask_url, verdict, spoilage_percent):
    url = f"{BACKEND_URL}/api/shipments/{SHIPMENT_ID}/inspections"
    payload = {
        "stepId": step_id,
        "imageUrl": image_url,
        "maskUrl": mask_url,
        "verdict": verdict,
        "spoilagePercent": spoilage_percent,
        "triggerType": "scheduled"
    }
 
    resp = requests.post(url, headers={"Authorization": f"Bearer {token}"}, json=payload, timeout=15)
 
    if resp.status_code == 201:
        print(f"[INSPECTION] Trimisa cu succes: {resp.json()['inspectionId']}")
    else:
        print(f"[INSPECTION] Eroare {resp.status_code}: {resp.text}")
 
 
# ============================================================
# INSPECTOR AI — THREAD PERIODIC
# ============================================================
def inspection_worker(maskrcnn_model, spoilage_model):
    status_map = {
        "Fresh": "Fresh",
        "Partially Spoiled": "Warning",
        "Heavily Spoiled": "Spoiled",
        "No apple detected": None
    }
 
    while True:
        print("[INSPECTION] Pornesc ciclul de inspectie AI...")
 
        if not capture_frame(CAMERA_URL, CAPTURE_PATH):
            print("[INSPECTION] Captura esuata, sar peste acest ciclu")
            time.sleep(INSPECTION_INTERVAL)
            continue
 
        result = run_ai_pipeline(maskrcnn_model, spoilage_model, CAPTURE_PATH)
        if result is None:
            print("[INSPECTION] Niciun fruct detectat, sar peste acest ciclu")
            time.sleep(INSPECTION_INTERVAL)
            continue
 
        analysis = result["analysis"]
        verdict = status_map.get(analysis["status"])
        print(f"[INSPECTION] Status: {analysis['status']} "
              f"(Spoiled: {analysis['spoiled_percentage']:.1f}%)")
 
        if verdict is None:
            time.sleep(INSPECTION_INTERVAL)
            continue
 
        if OFFLINE_MODE:
            print("[INSPECTION] Mod offline, nu trimit la backend")
            time.sleep(INSPECTION_INTERVAL)
            continue
 
        try:
            print("[UPLOAD] Uploading imagini pe catbox...")
            image_url = upload_to_catbox(result["segmented_path"])
            mask_url = upload_to_catbox(result["spoilage_path"])
 
            step_id = get_active_step()
            if step_id is None:
                print("[INSPECTION] Niciun step activ, nu trimit inspectia")
                time.sleep(INSPECTION_INTERVAL)
                continue
 
            token = ensure_token()
            send_inspection(
                token, step_id, image_url, mask_url,
                verdict, round(analysis["spoiled_percentage"], 2)
            )
        except Exception as e:
            print(f"[INSPECTION] Eroare la upload/raportare: {e}")
 
        time.sleep(INSPECTION_INTERVAL)
 
 
# ============================================================
# MAIN
# ============================================================
def main():
    print("[INIT] FreshLedger RPi Edge Device pornit")
    if OFFLINE_MODE:
        print("[INIT] *** MOD OFFLINE ***")
 
    get_id_token()
    if not OFFLINE_MODE:
        upload_pending_batches()
 
    print("[INIT] Incarc modelele AI (poate dura cateva secunde)...")
    maskrcnn_model = load_maskrcnn()
    spoilage_model = load_spoilage_model(SPOILAGE_MODEL_PATH)
    if spoilage_model is None:
        print("[INIT] Eroare fatala: modelul de spoilage nu a putut fi incarcat")
        return
 
    threading.Thread(target=gps_reader, daemon=True).start()
    threading.Thread(target=batch_sender, daemon=True).start()
    threading.Thread(target=inspection_worker, args=(maskrcnn_model, spoilage_model),
                      daemon=True).start()
 
    client = mqtt.Client()
    client.on_connect = on_connect
    client.on_message = on_message
    client.connect(MQTT_BROKER, MQTT_PORT, 60)
    client.loop_forever()
 
 
if __name__ == "__main__":
    main()
