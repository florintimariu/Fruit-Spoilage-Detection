#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "esp_check.h"
#include "esp_log.h"
#include "esp_timer.h"
#include "esp_rom_sys.h"
#include "nvs_flash.h"
#include "ha/esp_zigbee_ha_standard.h"
#include "zcl_utility.h"
#include "esp_zb_light.h"
#include "onewire_bus.h"
#include "ds18b20.h"

#define DATA_GPIO            2
#define TEMP_ENDPOINT        1
#define HUM_ENDPOINT         2
#define REPORT_INTERVAL_MS   5000
#define DHT22_RETRIES        3

#if !defined ZB_ED_ROLE
#error Define ZB_ED_ROLE in idf.py menuconfig to compile End Device source code.
#endif

static const char *TAG = "FRESHLEDGER";

typedef enum {
    SENSOR_NONE,
    SENSOR_DS18B20,
    SENSOR_DHT22,
} sensor_type_t;

typedef struct {
    float temperature;
    float humidity;
} dht22_reading_t;

static sensor_type_t detected_sensor = SENSOR_NONE;
static ds18b20_device_handle_t ds18b20_dev = NULL;

static void bdb_start_top_level_commissioning_cb(uint8_t mode_mask)
{
    ESP_RETURN_ON_FALSE(esp_zb_bdb_start_top_level_commissioning(mode_mask) == ESP_OK, ,
                       TAG, "Failed to start Zigbee commissioning");
}

void esp_zb_app_signal_handler(esp_zb_app_signal_t *signal_struct)
{
    uint32_t *p_sg_p = signal_struct->p_app_signal;
    esp_err_t err_status = signal_struct->esp_err_status;
    esp_zb_app_signal_type_t sig_type = *p_sg_p;

    switch (sig_type) {
    case ESP_ZB_ZDO_SIGNAL_SKIP_STARTUP:
        ESP_LOGI(TAG, "Initialize Zigbee stack");
        esp_zb_bdb_start_top_level_commissioning(ESP_ZB_BDB_MODE_INITIALIZATION);
        break;
    case ESP_ZB_BDB_SIGNAL_DEVICE_FIRST_START:
    case ESP_ZB_BDB_SIGNAL_DEVICE_REBOOT:
        if (err_status == ESP_OK) {
            ESP_LOGI(TAG, "Device started up in %s factory-reset mode",
                    esp_zb_bdb_is_factory_new() ? "" : "non");
            if (esp_zb_bdb_is_factory_new()) {
                ESP_LOGI(TAG, "Start network steering");
                esp_zb_bdb_start_top_level_commissioning(ESP_ZB_BDB_MODE_NETWORK_STEERING);
            } else {
                ESP_LOGI(TAG, "Device rebooted");
            }
        } else {
            ESP_LOGW(TAG, "Failed to initialize Zigbee stack (status: %s)",
                    esp_err_to_name(err_status));
        }
        break;
    case ESP_ZB_BDB_SIGNAL_STEERING:
        if (err_status == ESP_OK) {
            esp_zb_ieee_addr_t extended_pan_id;
            esp_zb_get_extended_pan_id(extended_pan_id);
            ESP_LOGI(TAG, "Joined network: PAN ID 0x%04hx, Channel %d, Short Address 0x%04hx",
                     esp_zb_get_pan_id(), esp_zb_get_current_channel(),
                     esp_zb_get_short_address());
        } else {
            ESP_LOGI(TAG, "Network steering failed (status: %s), retrying...",
                    esp_err_to_name(err_status));
            esp_zb_scheduler_alarm((esp_zb_callback_t)bdb_start_top_level_commissioning_cb,
                                  ESP_ZB_BDB_MODE_NETWORK_STEERING, 5000);
        }
        break;
    default:
        ESP_LOGI(TAG, "ZDO signal: %s (0x%x), status: %s",
                esp_zb_zdo_signal_to_string(sig_type), sig_type,
                esp_err_to_name(err_status));
        break;
    }
}

static esp_err_t dht22_read(gpio_num_t pin, dht22_reading_t *out)
{
    uint8_t data[5] = {0};
    int64_t t0;

    gpio_set_direction(pin, GPIO_MODE_OUTPUT);
    gpio_set_level(pin, 0);
    vTaskDelay(pdMS_TO_TICKS(2));
    gpio_set_level(pin, 1);
    esp_rom_delay_us(30);
    gpio_set_direction(pin, GPIO_MODE_INPUT);

    t0 = esp_timer_get_time();
    while (gpio_get_level(pin) == 1) {
        if (esp_timer_get_time() - t0 > 100) return ESP_FAIL;
    }
    t0 = esp_timer_get_time();
    while (gpio_get_level(pin) == 0) {
        if (esp_timer_get_time() - t0 > 100) return ESP_FAIL;
    }
    t0 = esp_timer_get_time();
    while (gpio_get_level(pin) == 1) {
        if (esp_timer_get_time() - t0 > 100) return ESP_FAIL;
    }

    for (int i = 0; i < 40; i++) {
        t0 = esp_timer_get_time();
        while (gpio_get_level(pin) == 0) {
            if (esp_timer_get_time() - t0 > 100) return ESP_FAIL;
        }
        int64_t high_start = esp_timer_get_time();
        t0 = esp_timer_get_time();
        while (gpio_get_level(pin) == 1) {
            if (esp_timer_get_time() - t0 > 200) return ESP_FAIL;
        }
        int64_t duration = esp_timer_get_time() - high_start;
        data[i / 8] <<= 1;
        if (duration > 40) {
            data[i / 8] |= 1;
        }
    }

    uint8_t checksum = data[0] + data[1] + data[2] + data[3];
    if (checksum != data[4]) {
        return ESP_ERR_INVALID_CRC;
    }

    out->humidity = ((data[0] << 8) | data[1]) / 10.0f;
    int16_t temp_raw = ((data[2] & 0x7F) << 8) | data[3];
    out->temperature = temp_raw / 10.0f;
    if (data[2] & 0x80) {
        out->temperature = -out->temperature;
    }

    return ESP_OK;
}

static sensor_type_t detect_sensor(void)
{
    ESP_LOGI(TAG, "Detecting sensor on GPIO %d...", DATA_GPIO);

    onewire_bus_handle_t bus = NULL;
    onewire_bus_config_t bus_config = {
        .bus_gpio_num = DATA_GPIO,
        .flags = { .en_pull_up = true },
    };
    onewire_bus_rmt_config_t rmt_config = { .max_rx_bytes = 10 };

    if (onewire_new_bus_rmt(&bus_config, &rmt_config, &bus) == ESP_OK) {
        onewire_device_iter_handle_t iter = NULL;
        onewire_device_t next_device;

        if (onewire_new_device_iter(bus, &iter) == ESP_OK) {
            if (onewire_device_iter_get_next(iter, &next_device) == ESP_OK) {
                ds18b20_config_t ds_cfg = {};
                if (ds18b20_new_device_from_enumeration(&next_device, &ds_cfg, &ds18b20_dev) == ESP_OK) {
                    onewire_del_device_iter(iter);
                    ESP_LOGI(TAG, "DS18B20 detected on the 1-Wire bus");
                    return SENSOR_DS18B20;
                }
            }
            onewire_del_device_iter(iter);
        }
    }

    ESP_LOGI(TAG, "No DS18B20 found, trying DHT22...");

    dht22_reading_t test_reading;
    for (int i = 0; i < DHT22_RETRIES; i++) {
        if (dht22_read(DATA_GPIO, &test_reading) == ESP_OK) {
            ESP_LOGI(TAG, "DHT22 detected");
            return SENSOR_DHT22;
        }
        vTaskDelay(pdMS_TO_TICKS(2000));
    }

    ESP_LOGE(TAG, "No known sensor detected on GPIO %d", DATA_GPIO);
    return SENSOR_NONE;
}

static void esp_zb_task(void *pvParameters)
{
    esp_zb_cfg_t zb_nwk_cfg = ESP_ZB_ZED_CONFIG();
    esp_zb_init(&zb_nwk_cfg);

    esp_zb_ep_list_t *ep_list = NULL;

    if (detected_sensor == SENSOR_DS18B20) {
        esp_zb_temperature_sensor_cfg_t sensor_cfg = ESP_ZB_DEFAULT_TEMPERATURE_SENSOR_CONFIG();
        ep_list = esp_zb_temperature_sensor_ep_create(TEMP_ENDPOINT, &sensor_cfg);

        zcl_basic_manufacturer_info_t info = {
            .manufacturer_name = "FreshLedger",
            .model_identifier = "DS18B20Node",
        };
        esp_zcl_utility_add_ep_basic_manufacturer_info(ep_list, TEMP_ENDPOINT, &info);

    } else if (detected_sensor == SENSOR_DHT22) {
        esp_zb_temperature_sensor_cfg_t temp_cfg = ESP_ZB_DEFAULT_TEMPERATURE_SENSOR_CONFIG();
        ep_list = esp_zb_temperature_sensor_ep_create(TEMP_ENDPOINT, &temp_cfg);

        zcl_basic_manufacturer_info_t temp_info = {
            .manufacturer_name = "FreshLedger",
            .model_identifier = "DHT22Node",
        };
        esp_zcl_utility_add_ep_basic_manufacturer_info(ep_list, TEMP_ENDPOINT, &temp_info);

        /* Humidity endpoint built manually: no shortcut *_ep_create
         * helper is available for the humidity sensor device type in
         * this SDK version, so the cluster list is assembled by hand
         * and appended to the existing ep_list as a second endpoint. */
        esp_zb_basic_cluster_cfg_t hum_basic_cfg = ESP_ZB_DEFAULT_BASIC_CLUSTER_CONFIG();
        esp_zb_identify_cluster_cfg_t hum_identify_cfg = ESP_ZB_DEFAULT_IDENTIFY_CLUSTER_CONFIG();
        esp_zb_humidity_meas_cluster_cfg_t humidity_cfg = ESP_ZB_DEFAULT_HUMIDITY_MEAS_CLUSTER_CONFIG();

        esp_zb_attribute_list_t *hum_basic_cluster = esp_zb_basic_cluster_create(&hum_basic_cfg);
        esp_zb_attribute_list_t *hum_identify_cluster = esp_zb_identify_cluster_create(&hum_identify_cfg);
        esp_zb_attribute_list_t *hum_humidity_cluster = esp_zb_humidity_meas_cluster_create(&humidity_cfg);

        esp_zb_cluster_list_t *hum_cluster_list = esp_zb_zcl_cluster_list_create();
        esp_zb_cluster_list_add_basic_cluster(hum_cluster_list, hum_basic_cluster, ESP_ZB_ZCL_CLUSTER_SERVER_ROLE);
        esp_zb_cluster_list_add_identify_cluster(hum_cluster_list, hum_identify_cluster, ESP_ZB_ZCL_CLUSTER_SERVER_ROLE);
        esp_zb_cluster_list_add_humidity_meas_cluster(hum_cluster_list, hum_humidity_cluster, ESP_ZB_ZCL_CLUSTER_SERVER_ROLE);

        esp_zb_endpoint_config_t hum_endpoint_config = {
            .endpoint = HUM_ENDPOINT,
            .app_profile_id = ESP_ZB_AF_HA_PROFILE_ID,
            .app_device_id = ESP_ZB_HA_HUMIDITY_SENSOR_DEVICE_ID,
            .app_device_version = 0
        };
        esp_zb_ep_list_add_ep(ep_list, hum_cluster_list, hum_endpoint_config);

        zcl_basic_manufacturer_info_t hum_info = {
            .manufacturer_name = "FreshLedger",
            .model_identifier = "DHT22Node",
        };
        esp_zcl_utility_add_ep_basic_manufacturer_info(ep_list, HUM_ENDPOINT, &hum_info);
    }

    esp_zb_device_register(ep_list);
    esp_zb_set_primary_network_channel_set(ESP_ZB_TRANSCEIVER_ALL_CHANNELS_MASK);
    ESP_ERROR_CHECK(esp_zb_start(false));
    esp_zb_stack_main_loop();
}

static void report_temperature(int16_t temp_x100)
{
    esp_zb_lock_acquire(portMAX_DELAY);

    esp_zb_zcl_set_attribute_val(
        TEMP_ENDPOINT,
        ESP_ZB_ZCL_CLUSTER_ID_TEMP_MEASUREMENT,
        ESP_ZB_ZCL_CLUSTER_SERVER_ROLE,
        ESP_ZB_ZCL_ATTR_TEMP_MEASUREMENT_VALUE_ID,
        &temp_x100,
        false
    );

    esp_zb_zcl_report_attr_cmd_t report_cmd = {
        .zcl_basic_cmd = {
            .dst_addr_u.addr_short = 0x0000,
            .dst_endpoint = 1,
            .src_endpoint = TEMP_ENDPOINT,
        },
        .address_mode = ESP_ZB_APS_ADDR_MODE_16_ENDP_PRESENT,
        .clusterID = ESP_ZB_ZCL_CLUSTER_ID_TEMP_MEASUREMENT,
        .direction = ESP_ZB_ZCL_CMD_DIRECTION_TO_CLI,
        .attributeID = ESP_ZB_ZCL_ATTR_TEMP_MEASUREMENT_VALUE_ID,
    };
    esp_zb_zcl_report_attr_cmd_req(&report_cmd);

    esp_zb_lock_release();
}

static void report_humidity(uint16_t hum_x100)
{
    esp_zb_lock_acquire(portMAX_DELAY);

    esp_zb_zcl_set_attribute_val(
        HUM_ENDPOINT,
        ESP_ZB_ZCL_CLUSTER_ID_REL_HUMIDITY_MEASUREMENT,
        ESP_ZB_ZCL_CLUSTER_SERVER_ROLE,
        ESP_ZB_ZCL_ATTR_REL_HUMIDITY_MEASUREMENT_VALUE_ID,
        &hum_x100,
        false
    );

    esp_zb_zcl_report_attr_cmd_t report_cmd = {
        .zcl_basic_cmd = {
            .dst_addr_u.addr_short = 0x0000,
            .dst_endpoint = 1,
            .src_endpoint = HUM_ENDPOINT,
        },
        .address_mode = ESP_ZB_APS_ADDR_MODE_16_ENDP_PRESENT,
        .clusterID = ESP_ZB_ZCL_CLUSTER_ID_REL_HUMIDITY_MEASUREMENT,
        .direction = ESP_ZB_ZCL_CMD_DIRECTION_TO_CLI,
        .attributeID = ESP_ZB_ZCL_ATTR_REL_HUMIDITY_MEASUREMENT_VALUE_ID,
    };
    esp_zb_zcl_report_attr_cmd_req(&report_cmd);

    esp_zb_lock_release();
}

static void ds18b20_task(void *pvParameters)
{
    vTaskDelay(pdMS_TO_TICKS(5000));

    while (1) {
        if (ds18b20_trigger_temperature_conversion(ds18b20_dev) == ESP_OK) {
            vTaskDelay(pdMS_TO_TICKS(800));
            float temperature;
            if (ds18b20_get_temperature(ds18b20_dev, &temperature) == ESP_OK) {
                ESP_LOGI(TAG, "DS18B20 Temperature: %.2f°C", temperature);
                report_temperature((int16_t)(temperature * 100));
            }
        }
        vTaskDelay(pdMS_TO_TICKS(REPORT_INTERVAL_MS));
    }
}

static void dht22_task(void *pvParameters)
{
    vTaskDelay(pdMS_TO_TICKS(5000));

    while (1) {
        dht22_reading_t reading;
        if (dht22_read(DATA_GPIO, &reading) == ESP_OK) {
            ESP_LOGI(TAG, "DHT22 Temperature: %.2f°C | Humidity: %.2f%%",
                     reading.temperature, reading.humidity);
            report_temperature((int16_t)(reading.temperature * 100));
            vTaskDelay(pdMS_TO_TICKS(200));
            report_humidity((uint16_t)(reading.humidity * 100));
        } else {
            ESP_LOGW(TAG, "Failed to read DHT22 - check the wiring!");
        }
        vTaskDelay(pdMS_TO_TICKS(REPORT_INTERVAL_MS));
    }
}

void app_main(void)
{
    esp_zb_platform_config_t config = {
        .radio_config = ESP_ZB_DEFAULT_RADIO_CONFIG(),
        .host_config = ESP_ZB_DEFAULT_HOST_CONFIG(),
    };
    ESP_ERROR_CHECK(nvs_flash_init());
    ESP_ERROR_CHECK(esp_zb_platform_config(&config));

    detected_sensor = detect_sensor();

    if (detected_sensor == SENSOR_NONE) {
        ESP_LOGE(TAG, "Stopping: no valid sensor detected");
        return;
    }

    xTaskCreate(esp_zb_task, "Zigbee_main", 4096, NULL, 5, NULL);

    if (detected_sensor == SENSOR_DS18B20) {
        xTaskCreate(ds18b20_task, "ds18b20_task", 4096, NULL, 4, NULL);
    } else if (detected_sensor == SENSOR_DHT22) {
        xTaskCreate(dht22_task, "dht22_task", 4096, NULL, 4, NULL);
    }
}