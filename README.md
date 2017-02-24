# Unity to Sketchfab exporter

Unity editor wizard that exports unity object to Sketchfab using **glTF 2.0** Format

Exporter based on Unity-glTF-Exporter from https://github.com/tparisi/Unity-glTF-Exporter

## How to use it

Once the plugin is imported (from the unity package provided in [the last release here](https://github.com/sketchfab/Unity-glTF-Exporter/releases), or after having checked out this repo),
a new item should appear in the *Tools* menu. You can access the exporter by going through **Tools/Publish to Sketchfab** as shown in the following screenshot:


![alt tag](https://github.com/sketchfab/Unity-glTF-Exporter/blob/feature/gltf-update-2-0_D3D-2812/Resources/dropdown_menu.JPG)


The exporter uses oauth authentication with *username/password* workflow.
You need to log in with your Sketchfab account before continuing.
If you don't have a Sketchfab account, you can click on the helpers to be redirected to the [sign up page](https://sketchfab.com/signup).

When successfuly logged, you will be able to use the exporter.
Select the objects you want to export, fill the forms with model infos and then click the upload button.
The exporter will pack up everything and upload it on Sketchfab. You will be redirected to the model page when it's done.

If you have any issue, please use the [Report an issue](https://help.sketchfab.com/hc/en-us/requests/new?type=exporters&subject=Unity+Exporter) link to be redirected to an appropriate report form.

Supported Unity objects and features so far:
- Scene objects such as transforms and meshes
- PBR materials (both *Standard* and *Standard (Specular setup)* for metal/smoothness and specular/smoothness respectively). Other materials may also be exported but not with all their channels.
- Solid and skinning animation (note that custom scripts or *humanoid* skeletal animation are not exported yet)

Please note that camera, lights, custom scripts, shaders and post processes are not exported.

## Features
* [PBR Materials](#pbrmaterials)
* [Transparency type](#transparency)
* [Texture conversion](#texture)
* [Samples](#samples)

<a name="pbrmaterials"></a>
##PBR materials
glTF 2.0 core specification includes metal/roughness PBR material declaration. Specular/glossiness workflow is also available but kept under an extensions for now.
Note that it's still not merged in glTF core, so the info are only accessible from this PR: https://github.com/KhronosGroup/glTF/pull/830
(It will be updated when everything will be packed up in main glTF specification)

Examples:

The following example describes a Metallic-Roughness material:
```json
    "materials": [
        {
            "pbrMetallicRoughness": {
                "baseColorFactor": [1, 1, 1, 1],
                "baseColorTexture" : {
                    "index" : 0,
                    "texCoord" : 0
                },
                "roughnessFactor": 0,
                "metallicFactor": 0,
                "metallicRoughnessTexture" : {
                    "index" : 1,
                    "texCoord" : 0
                }
            },
            "normalFactor": 1,
            "normalTexture" : {
                "index" : 2,
                "texCoord" : 0
            },
            "occlusionFactor": 1,
            "occlusionTexture" : {
                "index" : 3,
                "texCoord" : 0
            },
            "emissiveFactor": [0, 0, 0, 1],
            "name": "Skin"
        },
```

It's composed of a set of PBR textures, under `pbrMetallicRoughness`, and a set of additionnal maps.
For specular/glossiness workflow, it's still kept under an extension
*Specular workflow:*
```json
{
    "extensions": {
        "KHR_materials_pbrSpecularGlossiness": {
            "diffuseFactor": [1, 1, 1, 1],
            "diffuseTexture" : {
                "index" : 1,
                "texCoord" : 0
            },
            "glossinessFactor": 1,
            "specularFactor": [0.2, 0.2, 0.2, 1],
            "specularGlossinessTexture" : {
                "index" : 2,
                "texCoord" : 0
            }
        }
    },
    "normalFactor": 1,
    "normalTexture" : {
        "index" : 3,
        "texCoord" : 0
    },
    "occlusionFactor": 1,
    "occlusionTexture" : {
        "index" : 4,
        "texCoord" : 0
    },
    "emissiveFactor": [0, 0, 0, 1],
    "name": "Character material"

```

<a name="texture"></a>
##Texture conversion

glTF specification considers OpenGL flipY flag being disabled for images (see this [implementation note](https://github.com/KhronosGroup/glTF/tree/master/specification/1.0#images)).

(For more details about Flip Y flag in WebGL, see [gl.UNPACK_FLIP_Y_WEBGL parameter](https://developer.mozilla.org/en-US/docs/Web/API/WebGLRenderingContext/pixelStorei)).

This flag is enabled for most softwares including Unity, so textures need to be flipped along Y axis in order to match glTF specification.
The exporter applies this operation on all the exported textures.

Moreover, unity uses smoothness and not roughness, so *alpha channel is inverted for RGBA Metallic/Smoothness textures*, also to match glTF specification.


<a name="transparency"></a>
##Transparency

In order to differenciate between transparency types in Unity, an `extra` metadata is added to the material.

It allows to know which `blendMode` is used and the `cutoff` value.

For now, `blendMode` valid values are `alphaMask` and `alphaBlend`.
```json
"extras": {
	"blendMode" : "alphaMask",
	"cutoff" : 0.5
},
```

<a name="samples"></a>
## Samples

Some samples exported using this plugin are available (and downloadable) on Sketchfab https://sketchfab.com/features/gltf.