# Unity-glTF-Exporter
Unity editor wizard that exports to glTF Format

The exporter contains two EditorWindows that are set in `Tools` menu:
* **Publish to Sketchfab**: export and publish Unity scene data to Sketchfab through glTF data.
* **Export to glTF**: export Unity scene data locally into glTF files.

[Get samples](#samples)

## Features
* [PBR Materials](#pbrmaterials)
* [Transparency type](#transparency)
* [Multi-uvs support](#multiuvs)
* [FlipY (texture flag)](#flipyflag)
* [Normal map +/-Y flag](#normalmapflag)

<a name="pbrmaterials"></a>
##PBR materials
The PBR material schema used here is an extended version of [FRAUNHOFER materials pbr extension](https://github.com/tsturm/glTF/tree/master/extensions/Vendor/FRAUNHOFER_materials_pbr) that includes
a few update for completion purpose:

- Split pbr workflow textures:
	- `metallicRoughnessTexture` => `metallicTexture` + `roughnessTexture`
	- `specularGlossinessTexture` => `specularTexture` + `glossinessTexture`

- Added channels:
	- `opacityTexture`
	- `normalTexture` and `bumpTexture` with their respective factors `normalFactor` and `bumpFactor`. Channels are exclusive
	- `emissiveTexture` and `emissiveFactor`
	- `aoTexture` and `aoFactor`

Examples:

*Specular workflow:*
```json
"material_specularPBR": {
    "extensions": {
        "FRAUNHOFER_materials_pbr": {
            "materialModel": "PBR_specular_glossiness",
            "values": {
                "diffuseFactor": [1, 1, 1, 1],
                "diffuseTexture": "plane_diffuse_textureid",
                "specularFactor": [0.2, 0.2, 0.2, 1],

				"glossinessFactor": 0.443,
                "opacityTexture": "plane_opacity_textureid",
                "specularTexture": "plane_spec_textureid",
                "glossinessTexture": "plane_gloss_textureid",
                "normalFactor": 1,
                "normalTexture": "plane_normal_textureid",
                "aoFactor": 1,
                "emissiveFactor": [1.0, 0.5, 0.5, 1],
                "emissiveTexture": "plane_emissive_textureid"
                }
            }
        },
        "name": "specularPBR"
    }
},

```

*Metallic workflow:*
```json
"material_metallicPBR": {
    "extensions": {
        "FRAUNHOFER_materials_pbr": {
            "materialModel": "PBR_metal_roughness",
            "values": {
                    "baseColorFactor" : [0.9117647, 0.9117647, 0.9117647, 1],
                    "baseColorTexture" : "plane_albedo_textureid",
                    "roughnessFactor" : 0.754,
                    "metallicTexture" : "plane_metallic_textureid",
                    "metallicFactor" :"1.0",
                    "roughnessTexture" : "plane_specular_textureid",
                    "bumpFactor": 1,
                    "bumpTexture": "plane_normal_textureid",
                    "aoFactor": 1,
                    "aoTexture": "plane_ao_textureid",
                    "emissiveFactor": [0, 0, 0, 1]
            }
        }
    },
    "name": "metallicPBR"
}
```

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

<a name="multiuvs"></a>
##Multi-uvs support

At the moment, glTF specification doesn't provide any way to declare which UV set in the geometry is used by a given texture.
See [this issue](https://github.com/KhronosGroup/glTF/issues/742) on glTF repository.

In Unity exports, this data is added to the material channels under the `semantic`field.
```json
"aoTexture": {
    "texture" : "texture_Lightmap-0_comp_light_9514",
    "semantic" : "TEXCOORD_4"
}
```

The value of `semantic` corresponds to the attributes key of the UV set used, in the mesh:
```json
"mesh_Plane001_9690": {
    "name": "mesh_Plane001_9690",
    "primitives": [ {
        "attributes": {
            "POSITION": "accessor_position_Plane001_9690",
            "NORMAL": "accessor_normal_Plane001_9690",
            "TEXCOORD_0": "accessor_uv0_Plane001_9690",
            "TEXCOORD_1": "accessor_uv1_Plane001_9690",
            "TEXCOORD_4": "accessor_uv4_Plane001_9690"
        },
        "indices": "accessor_indices_0_Plane001_9690",
        "material": "material_cube_1_11974",
        "mode": 4
    } ]
}

```

<a name="flipyflag"></a>
##FlipY (texture flag)

glTF specification considers OpenGL flipY flag being disabled for images (see [this implementation note](https://github.com/KhronosGroup/glTF/tree/master/specification/1.0#images)).

(For more details about Flip Y flag in WebGL, see [gl.UNPACK_FLIP_Y_WEBGL parameter](https://developer.mozilla.org/en-US/docs/Web/API/WebGLRenderingContext/pixelStorei) )

This flag is enabled for most softwares including Unity, so texture would be read upside down, so to keep this data in the exported glTF, the flag is added in texture data (see [this issue](https://github.com/KhronosGroup/glTF/issues/736) )

```json
"texture_plane_diffuse_jpg": {
    "format": 6408,
    "internalFormat": 6408,
    "flipY": true,
    "sampler": "sampler_0",
    "source": "monster_jpg",
    "target": 3553,
    "type": 5121
}
```

<a name="normalmapflag"></a>
## DirectX vs OpenGL (normal map yUp flag)

OpenGL and DirectX have two different ways to use *normal maps*.

In order to dissociate those case and know in which space the normal map is, the flag yUp is added as `extra` (that is the common way to add application specific metadata on glTF objects)

```json
"texture_01_-_Default_normal_23668": {
    "extras": {
        "yUp" : true
    },
    "format": 6408,
    "internalFormat": 6408,
    "flipY": true,
    "sampler": "sampler_1_0_m",
    "source": "image_01_-_Default_normal_23668",
    "target": 3553,
    "type": 5121
},
```

<a name="samples"></a>
## Samples

Some samples exported using this plugin are available (and downloadable) on Sketchfab https://sketchfab.com/features/gltf.

These glTF files contain all the additional features listed above.