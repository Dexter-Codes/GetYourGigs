# Mobile Bento theme

The native MAUI app uses warm ivory backgrounds, white cards, forest-green actions, and lime, lavender, and peach accents. This is a deliberate light palette, including native fields and system appearance.

- `Resources/Styles/Colors.xaml` owns the palette; `Styles.xaml` owns typography, flat controls, and pressed/disabled states. Both are merged by `App.xaml`.
- `BentoSurface` is a single rounded Border with a fine outline. Padding belongs to its content layout. Opposing neumorphic shadow layers have been removed.
- Home uses a large discovery tile alongside two smaller, functional My gigs and Saved shortcuts. Category filters scroll horizontally so results sit closer to the search controls. Listing cards separate identity and description from price/status.
- Sign-in and account controls are grouped into cards. Other forms, details, and messages share the same field, button, typography, and surface styles.
- SVG icons, branding, splash, chat bubbles, and Android platform colors match the palette. Short category labels are display-only; API filter values are unchanged.
- Existing compiled bindings, safe-area handling, account modes, API contracts, and navigation commands are retained.

Verification details are reported with each change. Simulator screenshots cover only the screen inspected, not all form interactions, accessibility text sizes, or physical devices.
