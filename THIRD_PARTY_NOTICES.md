# Sources and third-party components

Original code, procedural geometry, star positions, HUD design, and shader artwork in this repository are covered by the repository's MIT license. The downloaded maps and fonts listed below are licensed separately. No Halo/Microsoft assets, logos, fonts, or Asset Store files are included, and no affiliation is implied.

## Planet and sky maps

Textures by **Solar System Scope / INOVE**, from https://www.solarsystemscope.com/textures/, distributed under **Creative Commons Attribution 4.0 International (CC BY 4.0)**: https://creativecommons.org/licenses/by/4.0/ (legal terms: https://creativecommons.org/licenses/by/4.0/legalcode).

Downloaded October 7, 2026. Files in `Assets/SolarExplorer/Resources/Textures/`:

- `2k_mercury.jpg`
- `2k_venus_atmosphere.jpg`
- `2k_earth_daymap.jpg`
- `2k_earth_nightmap.jpg`
- `2k_earth_clouds.jpg`
- `2k_mars.jpg`
- `2k_jupiter.jpg`
- `2k_saturn.jpg`
- `2k_saturn_ring_alpha.png`
- `2k_uranus.jpg`
- `2k_neptune.jpg`
- `2k_sun.jpg`
- `8k_stars_milky_way.jpg`

The detail upgrade also includes these source files from the same provider and under the same license:

- `8k_mercury.jpg`
- `4k_venus_atmosphere.jpg`
- `8k_earth_daymap.jpg`
- `8k_earth_nightmap.jpg`
- `8k_earth_clouds.jpg`
- `2k_earth_normal_map.tif`
- `2k_earth_specular_map.tif`
- `8k_mars.jpg`
- `8k_jupiter.jpg`
- `8k_saturn.jpg`
- `8k_saturn_ring_alpha.png`
- `8k_sun.jpg`

Filenames are preserved from the provider. Despite the `8k_` names, the downloaded Jupiter, Saturn, and Sun surface files are actually 4096 × 2048. Earth, Mercury, and Mars surface maps are 8192 × 4096; Venus is 4096 × 2048, and the Saturn ring map is 8192 × 500. `AssetProvenance.json` records actual dimensions and SHA-256 hashes. Earlier 2K files are retained as optional lower-resolution alternatives.

Each source file is available at `https://www.solarsystemscope.com/textures/download/` followed by its filename. Source image files are unchanged. Unity compresses the imported textures and creates mipmaps. Runtime shaders add lighting, atmospheric effects, ring shadowing, and color/exposure adjustments to the sky; the procedural nebula is an original addition. These visual modifications are not Solar System Scope's original presentation. Keep this attribution and license link when redistributing the maps, including in a public repository or a packaged build's accompanying credits.

The provider describes its maps as based on NASA imagery and elevation data, with enhanced colors and some fictional terrain filling unmapped regions. Their license does not imply endorsement by Solar System Scope or NASA.

## Milky Way panorama

**Credit: ESO/S. Brunier**. Image: [The Milky Way panorama, eso0932a](https://www.eso.org/public/images/eso0932a/). Downloaded October 7, 2026 from https://cdn.eso.org/images/large/eso0932a.jpg and saved as `Assets/SolarExplorer/Resources/Textures/eso0932a_milky_way.jpg` (6000 × 3000 pixels).

Licensed under [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/), as described in [ESO's usage terms](https://www.eso.org/public/copyright/). The supplied 18-megapixel panorama is used; the separately restricted 800-megapixel original is not included. The downloaded image file is unchanged. Runtime shaders rotate the panorama, adjust exposure/contrast/color, and add procedural stars and a faint nebula treatment. These presentation changes are original to this project, and no ESO endorsement is implied. The on-screen footer identifies the artist and license; the source and license links are recorded here. Preserve the full credit and license when redistributing the project or its rendered imagery.

## Fonts and Unity components

The Barlow Regular and Semibold font files are by The Barlow Project Authors, copyright 2017, and are distributed under the SIL Open Font License 1.1, not the repository MIT license. The complete license is bundled at `Assets/SolarExplorer/Resources/Fonts/OFL.txt`. Source: https://github.com/google/fonts/tree/main/ofl/barlow

Rajdhani Semibold is copyright 2014 Indian Type Foundry and is distributed under the SIL Open Font License 1.1. The complete license is bundled at `Assets/SolarExplorer/Resources/Fonts/Rajdhani-OFL.txt`. Source: https://github.com/google/fonts/tree/main/ofl/rajdhani

Unity Editor, Unity runtime components, Unity UI (`com.unity.ugui`), and the fallback editor-provided font are governed by their respective Unity licenses. They are resolved or provided by Unity, not relicensed by this repository. Unity UI package details: https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/index.html

Numerical facts:

- https://nssdc.gsfc.nasa.gov/planetary/factsheet/
- https://nssdc.gsfc.nasa.gov/planetary/factsheet/planetfact_notes.html

Article references (original summaries, not copied website text):

- https://science.nasa.gov/sun/facts/
- https://science.nasa.gov/mercury/facts/
- https://science.nasa.gov/venus/venus-facts/
- https://science.nasa.gov/earth/facts/
- https://science.nasa.gov/mars/facts/
- https://science.nasa.gov/jupiter/jupiter-facts/
- https://science.nasa.gov/saturn/facts/
- https://science.nasa.gov/uranus/facts/
- https://science.nasa.gov/neptune/neptune-facts/

Articles were reviewed on October 8, 2026. Planetary numeric statistics were checked October 7, 2026 against NASA's March 18, 2025 table. The Sun uses separate rounded statistics from NASA Science. Descriptions and article sections are original summaries. No NASA endorsement is implied. Each in-app article ends with a link to its NASA source.
