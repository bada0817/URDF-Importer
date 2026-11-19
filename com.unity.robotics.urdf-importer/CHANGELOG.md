# Changelog

All notable changes to this repository will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.5.3-acmm.1] - 2026-11-19

Maintained fork by bada0817 (https://github.com/bada0817/URDF-Importer), based on 0.5.2. Not an official Unity release.

### Upgrade Notes
- Install from `https://github.com/bada0817/URDF-Importer.git?path=/com.unity.robotics.urdf-importer#v0.5.3-acmm.1`. The package name is unchanged, so remove any copy of the official package from `Packages/` first.
- Existing STL prefabs and VHACD collision meshes are now reused on re-import (as originally intended). After changing a mesh file, enable `Overwrite Existing Prefabs` once, preferably in a scene that does not contain robots using the same meshes.

### Known Issues
- Materials are still reused by name: changing a `<material>` color in the URDF does not update an existing material asset. Delete it from `Materials/` before re-importing.

### Changed
- Changed "Use URDF Values" to "Override URDF Values" in URDF Inertial scripts and updated behavior to match expectation (AIRO-1782/AIRO-1946, cherry-picked from the official `dev` branch)
- Unity 6: use `GraphicsSettings.defaultRenderPipeline` instead of the obsolete `renderPipelineAsset` (older editors keep the old property)
- Default `Controller`: only movable (non-fixed) joints are selectable, the first joint is highlighted at start, and the Input System package is supported alongside the legacy Input Manager (optional asmdef reference, define `URDF_IMPORTER_INPUT_SYSTEM`)

### Fixed
- Default `Controller` highlight: colors are stored per joint at start, renderers come from the link's `Visuals` child, all material slots are highlighted and restored, and glTF (glTFast `baseColorFactor`) materials can be highlighted (`MaterialExtensions` color property fallback). No crash with fewer than two movable joints.
- `RuntimeUrdf.AssetExists` always returned false on Windows because it compared `\`-separated importer paths with `/`-separated AssetDatabase paths. STL prefabs and VHACD meshes were regenerated on every import, ignoring `Overwrite Existing Prefabs`, and importing a second URDF that shares meshes could fail with `NullReferenceException` in `VHACD.GenerateConvexMeshes`.
- `IndexOutOfRangeException` in `UrdfOrigin.GetPositionFromUrdf` from `UrdfInertial` on Play for links whose `<inertial>` has no `<origin>` (Unity deserializes the missing origin as empty arrays). Origins with fewer than 3 values are now treated as unspecified.
- Added check for NaN values during inertia matrix's conversion to inertia tensor rotation (AIRO-1703, cherry-picked from the official `dev` branch).

## [0.5.2-preview] - 2022-02-01

Added Sonarqube scanner

### Fixed
Fixed inability to read relative file paths 

Correct Axis change issues in URDF Importer


## [0.5.1-preview] - 2021-10-04
Fixed bug with multiple references to the same mesh during import

Add the [Close Stale Issues](https://github.com/marketplace/actions/close-stale-issues) action

### Upgrade Notes

### Known Issues

### Added

Start supporting file:// type URI.

### Changed

### Deprecated

### Removed

### Fixed

## [0.5.0-preview] - 2021-07-15

### Upgrade Notes
Update third party notices

Upgrade namespace from RosSharp.Urdf to Unity.Robotics.UrdfImporter

### Known Issues

### Added
Adding badges to main README

### Changed
STL files will not be automatically processed to create .prefab files when copied into the Assets directory or when assets are reimported. Instead the processing happens during the URDF import and required .prefab files will be created if they don't exist already or if the "Overwrite Existing Prefabs" option is checked in the URDF Import settings dialog.

Renamed "URDF" to "Urdf" in class names, function names and source filenames.

Renamed RuntimeURDFImporter to RuntimeUrdfImporterExample for clarification

### Deprecated

### Removed

### Fixed
Bug where-in URDF Importer would throw an error when installed via Package Manager because it can't save prefabs to its own directories

Compile error "Plugin 'assimp.dll' is used from several locations" when creating a Universal Windows Platform build (#122) 

Fix no material issue in Export robot to URDF (#127)

Fix the inconsistent casing of meta files (#128)

## [0.4.0-preview] - 2021-05-27

Note: The logs below only presents the changes from 0.3.0-preview

### Upgrade Notes
Refactor the codebase and support for runtime URDF importing

### Known Issues

### Added
Save the generated cylinder meshes to a new folder, `Assets/URDF/GeneratedMeshes`, as primitive cylinders will have no associated .stl filename/path

Add a link to the Robotics forum, and add a config.yml to add a link in the Github Issues page

Add unit tests and test coverage reporting

### Changed

### Deprecated

### Removed

### Fixed
Correct collider rotation so that the collider meshes matches visual meshes for both Y-axis and Z-axis

Replace "../" with the expected "package://" and throw more warnings if things still don't match up
