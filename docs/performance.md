# Performance

Performance budgets for the first stable release:
- Search feedback target: under 150 ms for 10,000 local contacts on typical desktop hardware.
- UI typing debounce: 180 ms to avoid issuing a database query for every keystroke.
- Database writes: one transaction per contact aggregate update.
- Large lists: Avalonia `ListBox` virtualization is relied on; avoid replacing it with an unvirtualized panel.

Benchmarks are not claimed until they are executed on representative hardware. Indexes currently cover contact name/flags, phone number, and case-insensitive email lookup.
