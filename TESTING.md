# Validation checklist

Run in Unity 6000.3.22f1 with the supplied Built-in Render Pipeline settings.

## Import

- Package import and C# compilation finish without Console errors.
- `Solar Explorer > Validate Project` prints `SOLAR_VALIDATION_PASSED`.
- Open the supplied scene and enter Play Mode. There is one camera, one EventSystem, eight planets, a Sun, and a populated UI.
- Stop and restart Play Mode twice; no duplicate UI, camera, or objects appear.
- The high overview shows a large Sun left of center, eight planets, cyan elliptical orbital guides, a drifting asteroid belt, and the Milky Way. Some outer arcs extend off screen.
- The initial camera shows all eight planets from a high angle. Top view shows them from directly above; switching back restores the high composition.
- Try mouse drag, wheel, finger drag, and pinch in the overview: its pose must stay fixed. Select a planet and return; the chosen high/top overview is restored.

## Selection and camera

- Click the Sun and every planet's sphere, then every label and navigation button; the title and all four statistics match the selected planet.
- Select Earth, then try selecting Saturn and Mercury during travel. Earth must remain the destination. After arrival, selecting Saturn starts a new locked flight; subsequent input must again be ignored until arrival.
- Press 1 through 8 and both arrow keys. Press 0 to select the Sun. Left arrow from overview selects the Sun; right arrow from overview selects Mercury.
- Each selected planet is visible to the left of the panel. Saturn's rings fit at the default zoom.
- Right-drag to orbit, including above and below the planet, and test both scroll zoom limits.
- Press Esc and Home after arriving at a focused view to return to the overview. During approach, return, or travel between worlds, these keys and all other input must be ignored. Controls resume after arrival.
- Resize between 1440 × 900, 1920 × 1080, and 1024 × 640. Planets fit the overview; navigation and panel content remain usable.

## UI and state

- Clicking the fact panel never selects a planet behind it.
- Wheel input over the panel scrolls its text and does not change camera zoom. Scroll to the NASA source buttons, which open the correct pages.
- Starting a right-drag on the panel does not orbit the camera.
- Pause with Space and the button; planet spin, cloud drift, asteroid movement, and orbital highlights stop while selection and camera movement still work. Resume restores animation.
- Switch to instant camera motion and change planets; transitions snap while planet rotation and ambient effects continue. Pause stops the entire world's animation independently of the camera mode.
- Leave the overview idle for five seconds. Every planet rotates, the Sun turns, the belt drifts, and individual foreground rocks tumble. Pause and resume; no rock or planet jumps ahead by the paused duration.
- Each planet has a distinct surface. No objects render magenta, no shader errors appear, and ring transparency works.
- Rajdhani headings and Barlow body text render correctly. Facts scroll without overlapping; compact stat values remain readable.
- Toggle Paths off, select a planet, and return. Paths remain off until switched on again.
- Move between planet spheres and navigation buttons. The hovered world's brackets, label, and orbital path update without getting stuck on the previous world.
- Enter and leave close-up views. Orbital guides and asteroids fade smoothly; the selected planet and its facts remain clear.
- Assign an optional equirectangular texture or Built-in material before Play Mode; the planet displays it and remains selectable.
- Inspect Earth's clouds, lit rim, and night side; inspect Saturn's transparent textured rings and the planet's shadow across them.
- Inspect Earth terrain detail, ocean reflections, and cloud shadows; inspect the rings' shadow on Saturn. The atmosphere should fade into space instead of forming an opaque blue outline.
- Confirm the Sun retains surface detail with a compact corona and soft, animated prominence loops that grow and fade. In Linear color space, bloom should affect bright scene highlights while HUD text stays clear.
- Inspect the Game view at 1920 × 1080 and Scale 1×, or a player at the display's native resolution. Enlarging a low-resolution Game view cannot reveal extra texture detail. Confirm the desktop maps import as BC7 at their source-limited 2K/4K/8K size, with 16× anisotropic filtering.
- The initial high view should fill the screen with the main worlds; the Sun sits left of center, Earth occupies the lower middle, and Jupiter fills the right side. Top view remains useful for inspecting the entire arrangement.
- Try switching to instant motion while a flight is in progress; the control stays locked. After arrival, switching to instant motion is available again.
- Confirm the HUD does not block clicking empty space except where a control or information panel is present.

## Phone and iPad acceptance

These require a physical device; simulated editor checks do not replace them.

- Test phone portrait/landscape and iPad portrait/landscape, including rotation while focused and while the article is scrolled. Text, X, reset, header controls, and navigation remain inside the safe area.
- Tap all nine destinations. Tap another visible body while viewing one. Input during travel is ignored; another destination becomes selectable only after arrival.
- Drag a world with one finger; spread/pinch two fingers to zoom. Lifting either finger never selects a different world. A third finger does not cause a zoom jump.
- Start a one-finger drag directly on each selected world while the camera is still approaching it. The angle must remain unchanged. The held finger must stay ignored even after arrival until lifted. Start a new gesture after arrival and confirm rotation works, then reset.
- Watch individual Sun prominences for six seconds. They should visibly rise, ripple, fade away, and reappear elsewhere, with traveling bright plasma. They must not remain as fixed wire loops. Pause deliberately freezes them.
- Pinch out to the default focus distance; continue pinching to confirm the limit. Rotate and zoom in, then tap Reset View to restore the original framing. Tap X to return to the fixed overview; input stays locked during that return flight.
- Swipe each article all the way down. The NASA button appears only at the bottom and opens the matching source page. Confirm the camera stays still throughout scrolling, including touches that begin on the panel and move outside it.
- Swipe the navigation strip to reach Neptune and back to the Sun. Dragging the strip must not activate an intervening planet button.
- Confirm every header control responds to touch, and that article text remains readable without zooming the screen.
- Inspect the Sun for visible surface churning and evolving loops, then pause and resume. Animation should resume from the stopped phase.
- Background and resume the app during a gesture; the next touch must start cleanly.
- Test native safe areas, home indicator, external-source browser handoff, and rotation on the actual target devices.
- Profile GPU/frame time, memory, and temperature over a sustained session. Mobile textures use ASTC 6×6 capped at 4K; verify import and device support. Tune effects after measurements if needed.

## Build

- Build a desktop player with the supplied scene and repeat selection, zoom, orbit, pause, and source-link checks.
- Confirm no network is needed to browse facts after installation. Only clicking a source link opens the browser.
- Profile frame rate and graphics memory on the target computer at 1080p. High-resolution maps and HDR effects raise the graphics cost; the batch render checks are not a frame-rate benchmark.

These are acceptance checks to run, not a claim that they have already passed in Unity 6.
