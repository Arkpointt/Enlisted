# Feature Spec: Camp Life Simulation

This spec describes the “camp life” simulation layer in Enlisted v0.9.0. It moves away from complex hidden meters to the transparent **Company Needs** system and the **Native Game Menu** interface.

## Status (v0.9.0 Update)
- **Company Needs** system is the core simulation layer, tracking Readiness, Morale, Supplies, Equipment, and Rest.
- **Native Game Menu** provides the interface for camp activities (Rest, Train, Morale Boost, Equipment Check).
- **Custom UI is deleted**: No more multi-tab management screens; all actions are code-driven menus.

---

## Index
- [Overview](#overview)
- [The Five Company Needs](#the-five-company-needs)
- [Camp Activities (Native Menu)](#camp-activities-native-menu)
- [System Integrations](#system-integrations)
- [Technical Implementation](#technical-implementation)

---

## Overview
While enlisted, the state of your unit is tracked via **Company Needs**. These needs fluctuate based on your activities, orders, and the campaign context. They drive the flavor of your service and gate access to critical resources.

---

## The Five Company Needs

We track five core meters (0–100) for the enlisted lord's company:

1.  **Readiness**: Reflects the unit's combat preparation and tactical edge. Impacted by training and successful missions.
2.  **Morale**: The psychological state of the men. Impacted by victories, food quality, and social activities.
3.  **Supplies**: The availability of food and consumables. Impacted by resupply orders and consumption.
4.  **Equipment**: The condition and maintenance of weapons and armor. Impacted by combat wear and equipment checks.
5.  **Rest**: Recovery from physical exhaustion. Impacted by march tempo and camp rest actions.

### Critical Thresholds
-   **Excellent (80+)**: Grants bonuses to order success chance and reputation gains.
-   **Stable (30-80)**: Standard operating conditions.
-   **Critical (<30)**: Triggers warnings, penalties, and potentially negative events (e.g., equipment access restricted, desertion risk).

### Needs Prediction
The unit's requirements are predicted based on the **Strategic Context**. In contexts like "Grand Campaign" (coordinated offensive), the system forecasts higher Readiness and Supply requirements. "Winter Camp" or "Riding the Marches" (peacetime) prioritize Rest and Morale. Players can view these forecasts in the Reports menu to prioritize their camp activities.

---

## Camp Activities (Native Menu)

Camp activities are accessed via the **Camp** submenu in the main Enlisted Status menu. These activities consume **Fatigue** and impact **Company Needs**.

### Core Activities
-   **Rest & Recover**: Restores **Rest** and **Readiness**. Consumes time.
-   **Train Skills**: Improves your skills and unit **Readiness**. Consumes **Fatigue**.
-   **Boost Morale**: Improves **Morale** through social activities or distributed rewards. Consumes **Denars** or **Fatigue**.
-   **Equipment Check**: Improves **Equipment** condition. Consumes **Fatigue**.

---

## System Integrations

### Quartermaster Integration
-   **Supplies Need**: If Supplies < 30, the Quartermaster may restrict access to high-tier gear or resupply options.
-   **Equipment Need**: Low Equipment status makes maintenance actions more expensive.

### News & Reports
-   Significant changes in Company Needs are reported in the **Daily Brief** and the **Reports** menu.
-   Crossing critical thresholds triggers immediate notifications to the player.

### Orders System
-   Orders have "Company Need Effects" defined in their JSON consequences.
-   A successful patrol might boost **Readiness**, while a forced march might tank **Rest**.

---

## Technical Implementation

-   **Data Model**: `CompanyNeedsState.cs` stores the current values.
-   **Manager**: `CompanyNeedsManager.cs` provides static helpers for modification and threshold checking.
-   **Access**: `ScheduleBehavior.Instance.CompanyNeeds` is the primary access point for the state.
-   **UI**: `EnlistedMenuBehavior.cs` defines the native game menus for camp activities.
