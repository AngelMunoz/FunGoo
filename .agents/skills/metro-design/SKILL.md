---
name: metro-design-system
description: Implement Microsoft's authentic Metro design language in UIs, web apps, mobile apps, and desktop software. Use when designing, building, refactoring, or reviewing interfaces with Metro principles, typography-first layouts, content-over-chrome minimalism, glanceable UIs, or motion-driven spatial navigation.
---
## When to Use

* Building a new UI, app, or component library inspired by Metro or Swiss/wayfinding typography.

* Refactoring cluttered, chrome-heavy interfaces into clean, content-first layouts.

* Designing motion, transitions, and spatial navigation for touch, desktop, or 10-foot (TV) screens.

* Auditing an existing flat UI that feels lifeless, confusing, or mismatched to its form factor.

## Core Philosophy

Apply these foundational rules before choosing components or styling tokens:

1. **Practice fierce reduction:** Decide what matters most on the screen, make it big and clear, and remove everything else. Strip away borders, bevels, nested containers, and secondary features until only essential content and actions remain.

2. **Treat visuals, motion, and interaction as one gesture:** Never design static flat screens and bolt on animations later. When you remove chrome and shadows, motion and direct tactile feedback carry the depth, emotion, and affordances.

3. **Optimize for "glance and go":** Surface high-value information immediately so users can read status at a glance, act with minimal friction, and leave the screen quickly.

4. **Be authentically digital:** Embrace light, typography, and motion native to screens rather than imitating physical materials (paper, wood, leather, or glassy reflections).

## Implementation Steps

### 1. Establish Typographic Structure (Content, Not Chrome)

* Replace bounding boxes, card borders, dividers, and decorative panels with whitespace, alignment, and typographic scale.

* Use clean sans-serif type with clear size and weight contrast to establish hierarchy. Let large headers and spatial grids organize the view.

* Let users interact directly with content instead of wrapping it in heavy chrome, redundant toolbars, or always-visible scrollbars.

* Simplify iconography through progressive subtraction: strip icons down to their most universally recognizable silhouette.

### 2. Design Motion as "Instruction Without Words"

* Use spatial previews (such as panorama or pivot views where adjacent section headers peek in from the edge) so users naturally discover horizontal navigation without arrows or helper text.

* Implement continuity transitions that carry a shared element from the origin view into the destination view so users maintain spatial orientation.

* Use subtle multi-plane parallax between background and foreground layers to create depth without drop shadows or faux-3D chrome.

* Make every interaction immediately responsive: provide crisp tactile feedback on press, scroll, and list boundaries.

* Mask latency with fluid, hardware-accelerated transitions so views feel instantaneous rather than blocked by static spinners.

### 3. Adapt Congruently to Form Factor and Ergonomics

* Prioritize **congruence over rigid consistency**: apply the same Metro principles, not identical layouts, across different devices. Never copy mobile edge-swipes, full-screen touch tiles, or hidden corner triggers onto a mouse-and-keyboard desktop UI.

* Know where the app sits on the Metro continuum: use pure content-first layouts for glanceable, consumption, or quick-task flows, and adapt cleanly for dense productivity or specialized workflows instead of forcing rigid templates.

* Place high-frequency controls and search within natural reach (for example, bottom-aligned application bars on mobile screens) rather than burying navigation in top-corner hamburger menus.

* Preserve established user muscle memory and platform conventions. Do not remove familiar navigation anchors just to make a layout look cleaner.

* Leverage display hardware purposefully, such as true dark backgrounds on OLED screens for contrast and power efficiency.

### 4. Prototype in Motion and Validate Early

* Prototype primary flows with real motion and transitions before locking down static specs.

* Test on actual target hardware with real input methods (thumb, mouse, keyboard, or remote) to catch friction that static mockups hide.

* Iterate wherever users hesitate, mis-tap, or lose track of where they are in the navigation hierarchy.

## Verification Checklist

Before shipping a Metro implementation, verify every item:

* \[ \] **Chrome audit:** Every remaining border, box, or background fill serves a clear functional purpose; removing it would break scannability.

* \[ \] **Typography audit:** Page hierarchy and grouping are immediately clear from type scale, weight, and whitespace alone.

* \[ \] **Motion audit:** Transitions communicate spatial direction and continuity at smooth frame rates without blocking user input.

* \[ \] **Form-factor audit:** Navigation, hit targets, and layout density match the target input device (touch, pointer, keyboard, or remote).

* \[ \] **Ergonomics audit:** Primary mobile controls sit within thumb reach, touch targets are generously sized, system navigation buttons are not duplicated inside the app, and interactive controls remain accessible when on-screen keyboards open.

* \[ \] **Glanceability audit:** The primary status or next action is readable within one second without drilling through menus.

## Gotchas

* **Reducing Metro to squares and flat color:** Shipping static flat boxes without typography-led hierarchy or fluid motion produces a sterile, confusing UI rather than Metro.

* **Confusing consistency with congruence:** Forcing a mobile layout onto a large desktop monitor (or vice versa) breaks usability and creates resentment toward the design system.

* **Ignoring basic input pitfalls:** Undersized touch targets, duplicate back/home buttons, hidden corner gestures on desktop, and controls trapped behind virtual keyboards immediately undermine a minimal UI.

* **Over-animating:** Motion should clarify structure and feel fast. Slow, bouncy, or gratuitous animations violate the "clean, light, open, and fast" principle.
