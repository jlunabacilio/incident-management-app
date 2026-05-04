# Test Scenarios — Incident Management System

## Seed Data (Available in Dev Mode)

### Aircraft

| Tail Number | Model            | Status    |
|-------------|------------------|-----------|
| PP-XKA      | Boeing 737-800   | Available |
| PR-GTA      | Airbus A320      | Available |
| PT-MXA      | Embraer E175     | Available |

### Users

| Email               | Password      | Role           | User ID        |
|---------------------|---------------|----------------|----------------|
| tech@demo.com       | Password123!  | Technician     | usr_tech_001   |
| supervisor@demo.com | Password123!  | Supervisor     | usr_sup_001    |
| safety@demo.com     | Password123!  | SafetyOfficer  | usr_safety_001 |

### Valid Enums

| Field       | Accepted Values                          |
|-------------|------------------------------------------|
| Fault Type  | Mechanical, Electrical, Hydraulic, Avionics |
| Severity    | Low, Medium, High, Critical              |
| Status      | Open, InProgress, PendingReview, Resolved, Closed, Voided |

---

## Scenario 1: Create Incident (Happy Path)

1. Login as `tech@demo.com` / `Password123!`
2. Navigate to `/incidents/new`
3. Fill the form:
   - Tail Number: `PP-XKA`
   - Fault Type: `Mechanical`
   - Severity: `Medium`
   - Description: (at least 20 characters) e.g. "Left engine oil leak detected during pre-flight inspection on gate B4."
   - Steps to Reproduce: (at least 10 characters) e.g. "Run engine at idle for 5 minutes and inspect left nacelle."
   - Technician ID: `usr_tech_001`
   - Supervisor ID: `usr_sup_001`
4. Submit → Incident created with status **Open** and number `INC-2026-000001`

---

## Scenario 2: Create Incident — Aircraft Not in Registry (Error)

1. Login as `tech@demo.com`
2. Try to create an incident with tail number `XX-ZZZ`
3. **Expected:** Error — "Aircraft 'XX-ZZZ' not found in registry."

> Only `PP-XKA`, `PR-GTA`, and `PT-MXA` are valid.

---

## Scenario 3: Create Incident — Invalid Technician/Supervisor (Error)

1. Login as `tech@demo.com`
2. Try to create an incident with:
   - Technician ID: `usr_fake_999`
   - Supervisor ID: `usr_sup_001`
3. **Expected:** Error — "Technician 'usr_fake_999' not found."

---

## Scenario 4: Create Incident — Supervisor Role Validation (Error)

1. Login as `tech@demo.com`
2. Try to create an incident with:
   - Technician ID: `usr_tech_001`
   - Supervisor ID: `usr_tech_001` (a Technician, not a Supervisor)
3. **Expected:** Error — "User 'usr_tech_001' does not have the Supervisor role."

---

## Scenario 5: Create CRITICAL Incident — Auto-Ground Aircraft

1. Login as `tech@demo.com`
2. Create an incident with:
   - Tail Number: `PR-GTA`
   - Severity: `Critical`
   - (other fields valid)
3. **Expected:**
   - Incident created with status **Open**
   - Aircraft `PR-GTA` is automatically **Grounded**
   - Dashboard shows grounded aircraft alert

---

## Scenario 6: Incident State Flow (Full Lifecycle)

1. Login as `tech@demo.com`
   - Create incident → status: **Open**
   - Transition to **InProgress**
   - Transition to **PendingReview**
2. Login as `supervisor@demo.com`
   - Transition to **Resolved**
3. Login as `safety@demo.com`
   - Transition to **Closed**

---

## Scenario 7: Void Incident

1. Login as `tech@demo.com` → Create an incident (status: Open)
2. Login as `supervisor@demo.com`
3. Open the incident detail page
4. Click "Void Incident" → Enter justification (mandatory)
5. **Expected:** Status changes to **Voided**

---

## Scenario 8: Void Incident — Technician Cannot Void (Error)

1. Login as `tech@demo.com`
2. Open an incident in Open/InProgress status
3. **Expected:** "Void Incident" button is NOT visible (only Supervisor+ can void)

---

## Scenario 9: Validation — Description Too Short

1. Login as `tech@demo.com`
2. Try to create an incident with description: "Short" (< 20 chars)
3. **Expected:** Client-side error "Description must be at least 20 characters."

---

## Scenario 10: Validation — Steps Too Short

1. Login as `tech@demo.com`
2. Try to create an incident with steps: "Do it" (< 10 chars)
3. **Expected:** Client-side error "Steps must be at least 10 characters."

---

## Scenario 11: Unground Aircraft

1. Create a CRITICAL incident on `PT-MXA` (aircraft gets grounded)
2. Login as `safety@demo.com`
3. Open the incident detail page
4. Click "Unground" button
5. **Expected:** Aircraft `PT-MXA` status returns to Available

---

## Scenario 12: Unauthorized Access (No Token)

1. Open browser in incognito / clear localStorage
2. Navigate to `http://localhost:3000`
3. **Expected:** Redirected to `/login`
4. Directly call `GET http://localhost:5000/api/v1/incidents`
5. **Expected:** HTTP 401 Unauthorized

---

## Scenario 13: Photo Attachments

1. Login as `tech@demo.com`
2. Create an incident with 1–10 image files (JPEG, PNG, or WEBP, max 10 MB each)
3. **Expected:** Incident created, attachments visible on detail page

---

## Scenario 14: Photo Attachments — Exceeds Limit (Error)

1. Try to create an incident with 11 image files
2. **Expected:** Error — "Maximum 10 attachments allowed per incident."

---

## Scenario 15: Incident List with Filters

1. Login as any user
2. Navigate to `/incidents`
3. Filter by:
   - Tail Number: `PP-XKA`
   - Severity: `Critical`
   - Status: `Open`
4. **Expected:** Only matching incidents shown, total count displayed
5. Click "Reset" → All filters cleared, full list shown
