# Solar Gateway Prometheus Metric Proxy

Provides a "proxy" API that gathers metrics from a solar gateway on your LAN, for ingestion into the Prometheus ecosystem. The project has been designed to support multiple brand's solar systems, so "plugins" can be added for new systems.

The list of currently implemented solar gateways:
* Tesla
* Enphase

I've also checked in a few example assets for Docker Compose, Prometheus, and Grafana to show how I'm running it in my own home.

> [!WARNING]
> This app is currently designed to be run in a totally-local Prometheus environent behind a firewall. As such, there is no authentication enforced for callers of the API.

## Configuring

If you look at the `docker-compose.example.yml` file, there are a few <variables> that need to be filled in.

### Tesla Gateway
* `Tesla__Host` - The IP address of your Tesla Gateway
* `Tesla__Email` - The email of your Tesla account
* `Tesla__Password` - This is **NOT** the password for your Tesla account:
  * You can check here for details on how to find the password, and possibly change it later: https://www.tesla.com/support/energy/powerwall/own/connecting-network
  * For my Backup Gateway 2 system, the sticker was located under the main cover. It was the last 5 characters of the string labeled "PASSWORD".
  

## Running
```sh
$ docker build -t solarapiproxy .
$ docker run -it --rm -p 8080:80 solarapiproxy
```

## Example output
```sh
$ curl localhost:8080/metrics
```

```
# HELP solarapiproxy_tesla_gateway_status_start_time start_time
# TYPE solarapiproxy_tesla_gateway_status_start_time gauge
solarapiproxy_tesla_gateway_status_start_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_instant_average_voltage instant_average_voltage
# TYPE solarapiproxy_tesla_gateway_site_instant_average_voltage gauge
solarapiproxy_tesla_gateway_site_instant_average_voltage{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_i_c_current i_c_current
# TYPE solarapiproxy_tesla_gateway_battery_i_c_current gauge
solarapiproxy_tesla_gateway_battery_i_c_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_operation_backup_reserve_percent backup_reserve_percent
# TYPE solarapiproxy_tesla_gateway_operation_backup_reserve_percent gauge
solarapiproxy_tesla_gateway_operation_backup_reserve_percent{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_instant_total_current instant_total_current
# TYPE solarapiproxy_tesla_gateway_load_instant_total_current gauge
solarapiproxy_tesla_gateway_load_instant_total_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_i_c_current i_c_current
# TYPE solarapiproxy_tesla_gateway_load_i_c_current gauge
solarapiproxy_tesla_gateway_load_i_c_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_i_a_current i_a_current
# TYPE solarapiproxy_tesla_gateway_site_i_a_current gauge
solarapiproxy_tesla_gateway_site_i_a_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_siteinfo_nominal_system_energy_kWh nominal_system_energy_kWh
# TYPE solarapiproxy_tesla_gateway_siteinfo_nominal_system_energy_kWh gauge
solarapiproxy_tesla_gateway_siteinfo_nominal_system_energy_kWh{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_energy_exported energy_exported
# TYPE solarapiproxy_tesla_gateway_site_energy_exported gauge
solarapiproxy_tesla_gateway_site_energy_exported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_frequency frequency
# TYPE solarapiproxy_tesla_gateway_site_frequency gauge
solarapiproxy_tesla_gateway_site_frequency{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_instant_reactive_power instant_reactive_power
# TYPE solarapiproxy_tesla_gateway_solar_instant_reactive_power gauge
solarapiproxy_tesla_gateway_solar_instant_reactive_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_instant_total_current instant_total_current
# TYPE solarapiproxy_tesla_gateway_battery_instant_total_current gauge
solarapiproxy_tesla_gateway_battery_instant_total_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE solarapiproxy_tesla_gateway_solar_last_phase_voltage_communication_time gauge
solarapiproxy_tesla_gateway_solar_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE solarapiproxy_tesla_gateway_battery_last_phase_energy_communication_time gauge
solarapiproxy_tesla_gateway_battery_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_i_c_current i_c_current
# TYPE solarapiproxy_tesla_gateway_solar_i_c_current gauge
solarapiproxy_tesla_gateway_solar_i_c_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_last_communication_time last_communication_time
# TYPE solarapiproxy_tesla_gateway_solar_last_communication_time gauge
solarapiproxy_tesla_gateway_solar_last_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_i_b_current i_b_current
# TYPE solarapiproxy_tesla_gateway_solar_i_b_current gauge
solarapiproxy_tesla_gateway_solar_i_b_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_instant_apparent_power instant_apparent_power
# TYPE solarapiproxy_tesla_gateway_solar_instant_apparent_power gauge
solarapiproxy_tesla_gateway_solar_instant_apparent_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE solarapiproxy_tesla_gateway_battery_last_phase_voltage_communication_time gauge
solarapiproxy_tesla_gateway_battery_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE solarapiproxy_tesla_gateway_load_last_phase_voltage_communication_time gauge
solarapiproxy_tesla_gateway_load_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_last_phase_power_communication_time last_phase_power_communication_time
# TYPE solarapiproxy_tesla_gateway_battery_last_phase_power_communication_time gauge
solarapiproxy_tesla_gateway_battery_last_phase_power_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_num_meters_aggregated num_meters_aggregated
# TYPE solarapiproxy_tesla_gateway_battery_num_meters_aggregated gauge
solarapiproxy_tesla_gateway_battery_num_meters_aggregated{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_energy_imported energy_imported
# TYPE solarapiproxy_tesla_gateway_battery_energy_imported gauge
solarapiproxy_tesla_gateway_battery_energy_imported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_energy_exported energy_exported
# TYPE solarapiproxy_tesla_gateway_load_energy_exported gauge
solarapiproxy_tesla_gateway_load_energy_exported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_instant_power instant_power
# TYPE solarapiproxy_tesla_gateway_solar_instant_power gauge
solarapiproxy_tesla_gateway_solar_instant_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_i_a_current i_a_current
# TYPE solarapiproxy_tesla_gateway_battery_i_a_current gauge
solarapiproxy_tesla_gateway_battery_i_a_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_instant_total_current instant_total_current
# TYPE solarapiproxy_tesla_gateway_solar_instant_total_current gauge
solarapiproxy_tesla_gateway_solar_instant_total_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_instant_average_current instant_average_current
# TYPE solarapiproxy_tesla_gateway_solar_instant_average_current gauge
solarapiproxy_tesla_gateway_solar_instant_average_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_instant_average_voltage instant_average_voltage
# TYPE solarapiproxy_tesla_gateway_battery_instant_average_voltage gauge
solarapiproxy_tesla_gateway_battery_instant_average_voltage{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_instant_average_voltage instant_average_voltage
# TYPE solarapiproxy_tesla_gateway_load_instant_average_voltage gauge
solarapiproxy_tesla_gateway_load_instant_average_voltage{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_num_meters_aggregated num_meters_aggregated
# TYPE solarapiproxy_tesla_gateway_site_num_meters_aggregated gauge
solarapiproxy_tesla_gateway_site_num_meters_aggregated{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_instant_apparent_power instant_apparent_power
# TYPE solarapiproxy_tesla_gateway_site_instant_apparent_power gauge
solarapiproxy_tesla_gateway_site_instant_apparent_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE solarapiproxy_tesla_gateway_load_last_phase_energy_communication_time gauge
solarapiproxy_tesla_gateway_load_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_frequency frequency
# TYPE solarapiproxy_tesla_gateway_load_frequency gauge
solarapiproxy_tesla_gateway_load_frequency{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_energy_imported energy_imported
# TYPE solarapiproxy_tesla_gateway_solar_energy_imported gauge
solarapiproxy_tesla_gateway_solar_energy_imported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_num_meters_aggregated num_meters_aggregated
# TYPE solarapiproxy_tesla_gateway_solar_num_meters_aggregated gauge
solarapiproxy_tesla_gateway_solar_num_meters_aggregated{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_i_a_current i_a_current
# TYPE solarapiproxy_tesla_gateway_load_i_a_current gauge
solarapiproxy_tesla_gateway_load_i_a_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE solarapiproxy_tesla_gateway_solar_last_phase_energy_communication_time gauge
solarapiproxy_tesla_gateway_solar_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_last_phase_power_communication_time last_phase_power_communication_time
# TYPE solarapiproxy_tesla_gateway_solar_last_phase_power_communication_time gauge
solarapiproxy_tesla_gateway_solar_last_phase_power_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_instant_power instant_power
# TYPE solarapiproxy_tesla_gateway_site_instant_power gauge
solarapiproxy_tesla_gateway_site_instant_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_instant_reactive_power instant_reactive_power
# TYPE solarapiproxy_tesla_gateway_site_instant_reactive_power gauge
solarapiproxy_tesla_gateway_site_instant_reactive_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_instant_reactive_power instant_reactive_power
# TYPE solarapiproxy_tesla_gateway_load_instant_reactive_power gauge
solarapiproxy_tesla_gateway_load_instant_reactive_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_instant_average_voltage instant_average_voltage
# TYPE solarapiproxy_tesla_gateway_solar_instant_average_voltage gauge
solarapiproxy_tesla_gateway_solar_instant_average_voltage{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_frequency frequency
# TYPE solarapiproxy_tesla_gateway_battery_frequency gauge
solarapiproxy_tesla_gateway_battery_frequency{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_instant_power instant_power
# TYPE solarapiproxy_tesla_gateway_battery_instant_power gauge
solarapiproxy_tesla_gateway_battery_instant_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_powerwall_percentage percentage
# TYPE solarapiproxy_tesla_gateway_powerwall_percentage gauge
solarapiproxy_tesla_gateway_powerwall_percentage{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_timeout timeout
# TYPE solarapiproxy_tesla_gateway_solar_timeout gauge
solarapiproxy_tesla_gateway_solar_timeout{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_timeout timeout
# TYPE solarapiproxy_tesla_gateway_battery_timeout gauge
solarapiproxy_tesla_gateway_battery_timeout{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_i_b_current i_b_current
# TYPE solarapiproxy_tesla_gateway_battery_i_b_current gauge
solarapiproxy_tesla_gateway_battery_i_b_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE solarapiproxy_tesla_gateway_site_last_phase_voltage_communication_time gauge
solarapiproxy_tesla_gateway_site_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_energy_exported energy_exported
# TYPE solarapiproxy_tesla_gateway_solar_energy_exported gauge
solarapiproxy_tesla_gateway_solar_energy_exported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_i_c_current i_c_current
# TYPE solarapiproxy_tesla_gateway_site_i_c_current gauge
solarapiproxy_tesla_gateway_site_i_c_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_last_phase_power_communication_time last_phase_power_communication_time
# TYPE solarapiproxy_tesla_gateway_site_last_phase_power_communication_time gauge
solarapiproxy_tesla_gateway_site_last_phase_power_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_instant_power instant_power
# TYPE solarapiproxy_tesla_gateway_load_instant_power gauge
solarapiproxy_tesla_gateway_load_instant_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_timeout timeout
# TYPE solarapiproxy_tesla_gateway_site_timeout gauge
solarapiproxy_tesla_gateway_site_timeout{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_siteinfo_nominal_system_power_kW nominal_system_power_kW
# TYPE solarapiproxy_tesla_gateway_siteinfo_nominal_system_power_kW gauge
solarapiproxy_tesla_gateway_siteinfo_nominal_system_power_kW{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_siteinfo_max_site_meter_power_kW max_site_meter_power_kW
# TYPE solarapiproxy_tesla_gateway_siteinfo_max_site_meter_power_kW gauge
solarapiproxy_tesla_gateway_siteinfo_max_site_meter_power_kW{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_siteinfo_panel_max_current panel_max_current
# TYPE solarapiproxy_tesla_gateway_siteinfo_panel_max_current gauge
solarapiproxy_tesla_gateway_siteinfo_panel_max_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_i_b_current i_b_current
# TYPE solarapiproxy_tesla_gateway_site_i_b_current gauge
solarapiproxy_tesla_gateway_site_i_b_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_instant_average_current instant_average_current
# TYPE solarapiproxy_tesla_gateway_site_instant_average_current gauge
solarapiproxy_tesla_gateway_site_instant_average_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_instant_average_current instant_average_current
# TYPE solarapiproxy_tesla_gateway_load_instant_average_current gauge
solarapiproxy_tesla_gateway_load_instant_average_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_energy_imported energy_imported
# TYPE solarapiproxy_tesla_gateway_site_energy_imported gauge
solarapiproxy_tesla_gateway_site_energy_imported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_instant_apparent_power instant_apparent_power
# TYPE solarapiproxy_tesla_gateway_battery_instant_apparent_power gauge
solarapiproxy_tesla_gateway_battery_instant_apparent_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_siteinfo_min_site_meter_power_kW min_site_meter_power_kW
# TYPE solarapiproxy_tesla_gateway_siteinfo_min_site_meter_power_kW gauge
solarapiproxy_tesla_gateway_siteinfo_min_site_meter_power_kW{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_energy_exported energy_exported
# TYPE solarapiproxy_tesla_gateway_battery_energy_exported gauge
solarapiproxy_tesla_gateway_battery_energy_exported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_timeout timeout
# TYPE solarapiproxy_tesla_gateway_load_timeout gauge
solarapiproxy_tesla_gateway_load_timeout{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_instant_apparent_power instant_apparent_power
# TYPE solarapiproxy_tesla_gateway_load_instant_apparent_power gauge
solarapiproxy_tesla_gateway_load_instant_apparent_power{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_last_communication_time last_communication_time
# TYPE solarapiproxy_tesla_gateway_load_last_communication_time gauge
solarapiproxy_tesla_gateway_load_last_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_i_b_current i_b_current
# TYPE solarapiproxy_tesla_gateway_load_i_b_current gauge
solarapiproxy_tesla_gateway_load_i_b_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_siteinfo_max_system_power_kW max_system_power_kW
# TYPE solarapiproxy_tesla_gateway_siteinfo_max_system_power_kW gauge
solarapiproxy_tesla_gateway_siteinfo_max_system_power_kW{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_siteinfo_max_system_energy_kWh max_system_energy_kWh
# TYPE solarapiproxy_tesla_gateway_siteinfo_max_system_energy_kWh gauge
solarapiproxy_tesla_gateway_siteinfo_max_system_energy_kWh{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE solarapiproxy_tesla_gateway_site_last_phase_energy_communication_time gauge
solarapiproxy_tesla_gateway_site_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_i_a_current i_a_current
# TYPE solarapiproxy_tesla_gateway_solar_i_a_current gauge
solarapiproxy_tesla_gateway_solar_i_a_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_status_up_time_seconds up_time_seconds
# TYPE solarapiproxy_tesla_gateway_status_up_time_seconds gauge
solarapiproxy_tesla_gateway_status_up_time_seconds{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_instant_average_current instant_average_current
# TYPE solarapiproxy_tesla_gateway_battery_instant_average_current gauge
solarapiproxy_tesla_gateway_battery_instant_average_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_operation_mode mode
# TYPE solarapiproxy_tesla_gateway_operation_mode gauge
solarapiproxy_tesla_gateway_operation_mode{mode="self_consumption",Host="<host>"} <value>
solarapiproxy_tesla_gateway_operation_mode{mode="backup",Host="<host>"} <value>
solarapiproxy_tesla_gateway_operation_mode{mode="autonomous",Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_last_communication_time last_communication_time
# TYPE solarapiproxy_tesla_gateway_battery_last_communication_time gauge
solarapiproxy_tesla_gateway_battery_last_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_instant_total_current instant_total_current
# TYPE solarapiproxy_tesla_gateway_site_instant_total_current gauge
solarapiproxy_tesla_gateway_site_instant_total_current{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_solar_frequency frequency
# TYPE solarapiproxy_tesla_gateway_solar_frequency gauge
solarapiproxy_tesla_gateway_solar_frequency{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_last_phase_power_communication_time last_phase_power_communication_time
# TYPE solarapiproxy_tesla_gateway_load_last_phase_power_communication_time gauge
solarapiproxy_tesla_gateway_load_last_phase_power_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_site_last_communication_time last_communication_time
# TYPE solarapiproxy_tesla_gateway_site_last_communication_time gauge
solarapiproxy_tesla_gateway_site_last_communication_time{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_load_energy_imported energy_imported
# TYPE solarapiproxy_tesla_gateway_load_energy_imported gauge
solarapiproxy_tesla_gateway_load_energy_imported{Host="<host>"} <value>
# HELP solarapiproxy_tesla_gateway_battery_instant_reactive_power instant_reactive_power
# TYPE solarapiproxy_tesla_gateway_battery_instant_reactive_power gauge
solarapiproxy_tesla_gateway_battery_instant_reactive_power{Host="<host>"} <value>
```
