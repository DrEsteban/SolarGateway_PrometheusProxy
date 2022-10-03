# Tesla Gateway Prometheus Metric Proxy

Provides a "proxy" API that gathers metrics from a Tesla Solar Gateway, for ingestion into the Prometheus ecosystem.

> WARNING: This app is currently designed to be run in a totally-local Prometheus environent behind a NAT firewall. As such, there is no authentication enforced by this proxy metrics API.

## Running
```sh
$ cd API/
$ docker build -t teslagatewayproxy .
$ docker run -it --rm -p 8080:80 teslagatewayproxy
```

## Example output
```sh
$ curl localhost:8080/metrics
```

```
# HELP tesla_gateway_status_start_time start_time
# TYPE tesla_gateway_status_start_time gauge
tesla_gateway_status_start_time{Host="<host>"} <value>
# HELP tesla_gateway_site_instant_average_voltage instant_average_voltage
# TYPE tesla_gateway_site_instant_average_voltage gauge
tesla_gateway_site_instant_average_voltage{Host="<host>"} <value>
# HELP tesla_gateway_battery_i_c_current i_c_current
# TYPE tesla_gateway_battery_i_c_current gauge
tesla_gateway_battery_i_c_current{Host="<host>"} <value>
# HELP tesla_gateway_operation_backup_reserve_percent backup_reserve_percent
# TYPE tesla_gateway_operation_backup_reserve_percent gauge
tesla_gateway_operation_backup_reserve_percent{Host="<host>"} <value>
# HELP tesla_gateway_load_instant_total_current instant_total_current
# TYPE tesla_gateway_load_instant_total_current gauge
tesla_gateway_load_instant_total_current{Host="<host>"} <value>
# HELP tesla_gateway_load_i_c_current i_c_current
# TYPE tesla_gateway_load_i_c_current gauge
tesla_gateway_load_i_c_current{Host="<host>"} <value>
# HELP tesla_gateway_site_i_a_current i_a_current
# TYPE tesla_gateway_site_i_a_current gauge
tesla_gateway_site_i_a_current{Host="<host>"} <value>
# HELP tesla_gateway_siteinfo_nominal_system_energy_kWh nominal_system_energy_kWh
# TYPE tesla_gateway_siteinfo_nominal_system_energy_kWh gauge
tesla_gateway_siteinfo_nominal_system_energy_kWh{Host="<host>"} <value>
# HELP tesla_gateway_site_energy_exported energy_exported
# TYPE tesla_gateway_site_energy_exported gauge
tesla_gateway_site_energy_exported{Host="<host>"} <value>
# HELP tesla_gateway_site_frequency frequency
# TYPE tesla_gateway_site_frequency gauge
tesla_gateway_site_frequency{Host="<host>"} <value>
# HELP tesla_gateway_solar_instant_reactive_power instant_reactive_power
# TYPE tesla_gateway_solar_instant_reactive_power gauge
tesla_gateway_solar_instant_reactive_power{Host="<host>"} <value>
# HELP tesla_gateway_battery_instant_total_current instant_total_current
# TYPE tesla_gateway_battery_instant_total_current gauge
tesla_gateway_battery_instant_total_current{Host="<host>"} <value>
# HELP tesla_gateway_solar_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE tesla_gateway_solar_last_phase_voltage_communication_time gauge
tesla_gateway_solar_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_battery_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE tesla_gateway_battery_last_phase_energy_communication_time gauge
tesla_gateway_battery_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_solar_i_c_current i_c_current
# TYPE tesla_gateway_solar_i_c_current gauge
tesla_gateway_solar_i_c_current{Host="<host>"} <value>
# HELP tesla_gateway_solar_last_communication_time last_communication_time
# TYPE tesla_gateway_solar_last_communication_time gauge
tesla_gateway_solar_last_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_solar_i_b_current i_b_current
# TYPE tesla_gateway_solar_i_b_current gauge
tesla_gateway_solar_i_b_current{Host="<host>"} <value>
# HELP tesla_gateway_solar_instant_apparent_power instant_apparent_power
# TYPE tesla_gateway_solar_instant_apparent_power gauge
tesla_gateway_solar_instant_apparent_power{Host="<host>"} <value>
# HELP tesla_gateway_battery_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE tesla_gateway_battery_last_phase_voltage_communication_time gauge
tesla_gateway_battery_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_load_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE tesla_gateway_load_last_phase_voltage_communication_time gauge
tesla_gateway_load_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_battery_last_phase_power_communication_time last_phase_power_communication_time
# TYPE tesla_gateway_battery_last_phase_power_communication_time gauge
tesla_gateway_battery_last_phase_power_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_battery_num_meters_aggregated num_meters_aggregated
# TYPE tesla_gateway_battery_num_meters_aggregated gauge
tesla_gateway_battery_num_meters_aggregated{Host="<host>"} <value>
# HELP tesla_gateway_battery_energy_imported energy_imported
# TYPE tesla_gateway_battery_energy_imported gauge
tesla_gateway_battery_energy_imported{Host="<host>"} <value>
# HELP tesla_gateway_load_energy_exported energy_exported
# TYPE tesla_gateway_load_energy_exported gauge
tesla_gateway_load_energy_exported{Host="<host>"} <value>
# HELP tesla_gateway_solar_instant_power instant_power
# TYPE tesla_gateway_solar_instant_power gauge
tesla_gateway_solar_instant_power{Host="<host>"} <value>
# HELP tesla_gateway_battery_i_a_current i_a_current
# TYPE tesla_gateway_battery_i_a_current gauge
tesla_gateway_battery_i_a_current{Host="<host>"} <value>
# HELP tesla_gateway_solar_instant_total_current instant_total_current
# TYPE tesla_gateway_solar_instant_total_current gauge
tesla_gateway_solar_instant_total_current{Host="<host>"} <value>
# HELP tesla_gateway_solar_instant_average_current instant_average_current
# TYPE tesla_gateway_solar_instant_average_current gauge
tesla_gateway_solar_instant_average_current{Host="<host>"} <value>
# HELP tesla_gateway_battery_instant_average_voltage instant_average_voltage
# TYPE tesla_gateway_battery_instant_average_voltage gauge
tesla_gateway_battery_instant_average_voltage{Host="<host>"} <value>
# HELP tesla_gateway_load_instant_average_voltage instant_average_voltage
# TYPE tesla_gateway_load_instant_average_voltage gauge
tesla_gateway_load_instant_average_voltage{Host="<host>"} <value>
# HELP tesla_gateway_site_num_meters_aggregated num_meters_aggregated
# TYPE tesla_gateway_site_num_meters_aggregated gauge
tesla_gateway_site_num_meters_aggregated{Host="<host>"} <value>
# HELP tesla_gateway_site_instant_apparent_power instant_apparent_power
# TYPE tesla_gateway_site_instant_apparent_power gauge
tesla_gateway_site_instant_apparent_power{Host="<host>"} <value>
# HELP tesla_gateway_load_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE tesla_gateway_load_last_phase_energy_communication_time gauge
tesla_gateway_load_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_load_frequency frequency
# TYPE tesla_gateway_load_frequency gauge
tesla_gateway_load_frequency{Host="<host>"} <value>
# HELP tesla_gateway_solar_energy_imported energy_imported
# TYPE tesla_gateway_solar_energy_imported gauge
tesla_gateway_solar_energy_imported{Host="<host>"} <value>
# HELP tesla_gateway_solar_num_meters_aggregated num_meters_aggregated
# TYPE tesla_gateway_solar_num_meters_aggregated gauge
tesla_gateway_solar_num_meters_aggregated{Host="<host>"} <value>
# HELP tesla_gateway_load_i_a_current i_a_current
# TYPE tesla_gateway_load_i_a_current gauge
tesla_gateway_load_i_a_current{Host="<host>"} <value>
# HELP tesla_gateway_solar_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE tesla_gateway_solar_last_phase_energy_communication_time gauge
tesla_gateway_solar_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_solar_last_phase_power_communication_time last_phase_power_communication_time
# TYPE tesla_gateway_solar_last_phase_power_communication_time gauge
tesla_gateway_solar_last_phase_power_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_site_instant_power instant_power
# TYPE tesla_gateway_site_instant_power gauge
tesla_gateway_site_instant_power{Host="<host>"} <value>
# HELP tesla_gateway_site_instant_reactive_power instant_reactive_power
# TYPE tesla_gateway_site_instant_reactive_power gauge
tesla_gateway_site_instant_reactive_power{Host="<host>"} <value>
# HELP tesla_gateway_load_instant_reactive_power instant_reactive_power
# TYPE tesla_gateway_load_instant_reactive_power gauge
tesla_gateway_load_instant_reactive_power{Host="<host>"} <value>
# HELP tesla_gateway_solar_instant_average_voltage instant_average_voltage
# TYPE tesla_gateway_solar_instant_average_voltage gauge
tesla_gateway_solar_instant_average_voltage{Host="<host>"} <value>
# HELP tesla_gateway_battery_frequency frequency
# TYPE tesla_gateway_battery_frequency gauge
tesla_gateway_battery_frequency{Host="<host>"} <value>
# HELP tesla_gateway_battery_instant_power instant_power
# TYPE tesla_gateway_battery_instant_power gauge
tesla_gateway_battery_instant_power{Host="<host>"} <value>
# HELP tesla_gateway_powerwall_percentage percentage
# TYPE tesla_gateway_powerwall_percentage gauge
tesla_gateway_powerwall_percentage{Host="<host>"} <value>
# HELP tesla_gateway_solar_timeout timeout
# TYPE tesla_gateway_solar_timeout gauge
tesla_gateway_solar_timeout{Host="<host>"} <value>
# HELP tesla_gateway_battery_timeout timeout
# TYPE tesla_gateway_battery_timeout gauge
tesla_gateway_battery_timeout{Host="<host>"} <value>
# HELP tesla_gateway_battery_i_b_current i_b_current
# TYPE tesla_gateway_battery_i_b_current gauge
tesla_gateway_battery_i_b_current{Host="<host>"} <value>
# HELP tesla_gateway_site_last_phase_voltage_communication_time last_phase_voltage_communication_time
# TYPE tesla_gateway_site_last_phase_voltage_communication_time gauge
tesla_gateway_site_last_phase_voltage_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_solar_energy_exported energy_exported
# TYPE tesla_gateway_solar_energy_exported gauge
tesla_gateway_solar_energy_exported{Host="<host>"} <value>
# HELP tesla_gateway_site_i_c_current i_c_current
# TYPE tesla_gateway_site_i_c_current gauge
tesla_gateway_site_i_c_current{Host="<host>"} <value>
# HELP tesla_gateway_site_last_phase_power_communication_time last_phase_power_communication_time
# TYPE tesla_gateway_site_last_phase_power_communication_time gauge
tesla_gateway_site_last_phase_power_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_load_instant_power instant_power
# TYPE tesla_gateway_load_instant_power gauge
tesla_gateway_load_instant_power{Host="<host>"} <value>
# HELP tesla_gateway_site_timeout timeout
# TYPE tesla_gateway_site_timeout gauge
tesla_gateway_site_timeout{Host="<host>"} <value>
# HELP tesla_gateway_siteinfo_nominal_system_power_kW nominal_system_power_kW
# TYPE tesla_gateway_siteinfo_nominal_system_power_kW gauge
tesla_gateway_siteinfo_nominal_system_power_kW{Host="<host>"} <value>
# HELP tesla_gateway_siteinfo_max_site_meter_power_kW max_site_meter_power_kW
# TYPE tesla_gateway_siteinfo_max_site_meter_power_kW gauge
tesla_gateway_siteinfo_max_site_meter_power_kW{Host="<host>"} <value>
# HELP tesla_gateway_siteinfo_panel_max_current panel_max_current
# TYPE tesla_gateway_siteinfo_panel_max_current gauge
tesla_gateway_siteinfo_panel_max_current{Host="<host>"} <value>
# HELP tesla_gateway_site_i_b_current i_b_current
# TYPE tesla_gateway_site_i_b_current gauge
tesla_gateway_site_i_b_current{Host="<host>"} <value>
# HELP tesla_gateway_site_instant_average_current instant_average_current
# TYPE tesla_gateway_site_instant_average_current gauge
tesla_gateway_site_instant_average_current{Host="<host>"} <value>
# HELP tesla_gateway_load_instant_average_current instant_average_current
# TYPE tesla_gateway_load_instant_average_current gauge
tesla_gateway_load_instant_average_current{Host="<host>"} <value>
# HELP tesla_gateway_site_energy_imported energy_imported
# TYPE tesla_gateway_site_energy_imported gauge
tesla_gateway_site_energy_imported{Host="<host>"} <value>
# HELP tesla_gateway_battery_instant_apparent_power instant_apparent_power
# TYPE tesla_gateway_battery_instant_apparent_power gauge
tesla_gateway_battery_instant_apparent_power{Host="<host>"} <value>
# HELP tesla_gateway_siteinfo_min_site_meter_power_kW min_site_meter_power_kW
# TYPE tesla_gateway_siteinfo_min_site_meter_power_kW gauge
tesla_gateway_siteinfo_min_site_meter_power_kW{Host="<host>"} <value>
# HELP tesla_gateway_battery_energy_exported energy_exported
# TYPE tesla_gateway_battery_energy_exported gauge
tesla_gateway_battery_energy_exported{Host="<host>"} <value>
# HELP tesla_gateway_load_timeout timeout
# TYPE tesla_gateway_load_timeout gauge
tesla_gateway_load_timeout{Host="<host>"} <value>
# HELP tesla_gateway_load_instant_apparent_power instant_apparent_power
# TYPE tesla_gateway_load_instant_apparent_power gauge
tesla_gateway_load_instant_apparent_power{Host="<host>"} <value>
# HELP tesla_gateway_load_last_communication_time last_communication_time
# TYPE tesla_gateway_load_last_communication_time gauge
tesla_gateway_load_last_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_load_i_b_current i_b_current
# TYPE tesla_gateway_load_i_b_current gauge
tesla_gateway_load_i_b_current{Host="<host>"} <value>
# HELP tesla_gateway_siteinfo_max_system_power_kW max_system_power_kW
# TYPE tesla_gateway_siteinfo_max_system_power_kW gauge
tesla_gateway_siteinfo_max_system_power_kW{Host="<host>"} <value>
# HELP tesla_gateway_siteinfo_max_system_energy_kWh max_system_energy_kWh
# TYPE tesla_gateway_siteinfo_max_system_energy_kWh gauge
tesla_gateway_siteinfo_max_system_energy_kWh{Host="<host>"} <value>
# HELP tesla_gateway_site_last_phase_energy_communication_time last_phase_energy_communication_time
# TYPE tesla_gateway_site_last_phase_energy_communication_time gauge
tesla_gateway_site_last_phase_energy_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_solar_i_a_current i_a_current
# TYPE tesla_gateway_solar_i_a_current gauge
tesla_gateway_solar_i_a_current{Host="<host>"} <value>
# HELP tesla_gateway_status_up_time_seconds up_time_seconds
# TYPE tesla_gateway_status_up_time_seconds gauge
tesla_gateway_status_up_time_seconds{Host="<host>"} <value>
# HELP tesla_gateway_battery_instant_average_current instant_average_current
# TYPE tesla_gateway_battery_instant_average_current gauge
tesla_gateway_battery_instant_average_current{Host="<host>"} <value>
# HELP tesla_gateway_operation_mode mode
# TYPE tesla_gateway_operation_mode gauge
tesla_gateway_operation_mode{mode="self_consumption",Host="<host>"} <value>
tesla_gateway_operation_mode{mode="backup",Host="<host>"} <value>
tesla_gateway_operation_mode{mode="autonomous",Host="<host>"} <value>
# HELP tesla_gateway_battery_last_communication_time last_communication_time
# TYPE tesla_gateway_battery_last_communication_time gauge
tesla_gateway_battery_last_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_site_instant_total_current instant_total_current
# TYPE tesla_gateway_site_instant_total_current gauge
tesla_gateway_site_instant_total_current{Host="<host>"} <value>
# HELP tesla_gateway_solar_frequency frequency
# TYPE tesla_gateway_solar_frequency gauge
tesla_gateway_solar_frequency{Host="<host>"} <value>
# HELP tesla_gateway_load_last_phase_power_communication_time last_phase_power_communication_time
# TYPE tesla_gateway_load_last_phase_power_communication_time gauge
tesla_gateway_load_last_phase_power_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_site_last_communication_time last_communication_time
# TYPE tesla_gateway_site_last_communication_time gauge
tesla_gateway_site_last_communication_time{Host="<host>"} <value>
# HELP tesla_gateway_load_energy_imported energy_imported
# TYPE tesla_gateway_load_energy_imported gauge
tesla_gateway_load_energy_imported{Host="<host>"} <value>
# HELP tesla_gateway_battery_instant_reactive_power instant_reactive_power
# TYPE tesla_gateway_battery_instant_reactive_power gauge
tesla_gateway_battery_instant_reactive_power{Host="<host>"} <value>
```

## Known Issues / Desired improvements
* I originally wanted to use a standard HttpClient to fetch the metrics, but unfortunately .NET Core 6.0 seems to have a bug on Linux where there's no way to tell it to ignore TLS cert validation. Since the Tesla Gateway hosts its API with a self-signed cert, and I wanted this to run in Docker, that meant I had to go with a hacky solution of farming the API calls out to `curl --insecure`. If/when Microsoft fixes this I'd like to go back to an HttpClient-based implementation.
  * An HttpClient-based implementation _works_ on Windows, just not Linux/Docker...
