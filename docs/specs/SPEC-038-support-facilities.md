# SPEC-038: Support facilities

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Base & economy (touches AI relationship)
- Touches: power (sources, day/night), fuel storage, corruption from compute, memory restoration (SPEC-008)
- Source rules: doc 02 s3 (solar fields: slow, safe, fragile), doc 02 s6 (Power & fuel: fuel depots, solar fields;
  AI core: cooling, memory restoration chambers)

## Goal
More ways to shape the Hub: free daylight power that leaves nights to the generators, a fuel reserve for vehicles and
the reactor, cooling that lets the handler lean on compute with less corruption, and a chamber that brings the AI's
memory back sooner (and with it the next tier).

## Rules (tables in `[facility_solar_field]`, `[facility_fuel_depot]`, `[facility_cooling_tower]`, `[facility_memory_chamber]`)
1. **Solar Field**: a power source with no upkeep or fuel; full output 07:00-18:00, half at 06:00 and 18:00, none at
   night; takes battle damage like other producers.
2. **Fuel Depot**: adds its output to the fuel cap while running.
3. **Cooling Tower**: corruption from compute use drops by the towers' output in percent (all together at most 60%).
4. **Memory Restoration Chamber**: memory-lane (M1-M3) restoration is faster by the chambers' output in percent
   (all together at most 60%).
5. All four are ordinary facilities: energy, crew, levels, priority, scars. No new state.

## Tests
`EconomyTests.SolarFieldsFollowTheSun_AndDepotsHoldMoreFuel`.
