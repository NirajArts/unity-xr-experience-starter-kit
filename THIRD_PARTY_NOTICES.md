# Third-party notices

This repository references packages through Unity Package Manager. Package caches and SDK source are **not** redistributed in this repository. The package manager installs their own LICENSE and Third Party Notices files; retain them when redistributing anything from those packages.

| Origin | Used for | Governing terms / source |
|---|---|---|
| Meta XR Core, Interaction, Interaction OVR 77.0.0 | OVRCameraRig, hand/controller tracking, synthetic visuals, grab/ray/poke, package pose references | [Oculus SDK License Agreement](https://developer.oculus.com/licenses/oculussdk/); each package's `LICENSE.md` and third-party notices |
| Unity OpenXR, XR Management, Core Utils, Input System, UGUI, TMP, Test Framework and built-in modules | XR loading, input/UI, rendering and testing | Each package's LICENSE and third-party notices in its supported UPM distribution; Unity Editor/runtime terms remain applicable |
| NUnit through Unity's `com.unity.ext.nunit` | Tests | Installed package's NUnit license/third-party notices |
| Unity built-in primitives, shaders and runtime font | Demo geometry, materials and labels | Unity's applicable software/content terms; not asserted to be original third-party assets |

The rig and canvas objects retain references to official package prefabs. Grip base poses also reference the installed Meta package. These links do not grant ownership of those packages or relicense them under MIT.

New modular meshes, layout, runtime code and six feedback WAVs were created for this kit. Owner-authored Grip Editor and bounded bridge code were reused with explicit authorization. No client recordings, supplied graphics, production models or Asset Store content were copied. Review the terms of any content you add before distributing a derivative, including on an asset marketplace.
