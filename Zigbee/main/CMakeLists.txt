idf_component_register(
    SRC_DIRS  "."
    INCLUDE_DIRS "."
    PRIV_REQUIRES nvs_flash esp_driver_uart ieee802154 onewire_bus ds18b20 esp_timer driver
)