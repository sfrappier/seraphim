"""Offline derivatives only. Run with the pinned portable Blender via Prepare-CharacterModels.ps1.

Original archives stay in .cache. Unity builds consume the checked-in FBX/PNG derivatives,
so neither Blender nor a Sketchfab login is required to build a clean checkout.
"""
import bpy
import hashlib
import json
import math
import re
import sys
from pathlib import Path

import numpy as np
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / 'Assets/ThirdParty/CCBY/Characters'
INPUTS = ROOT / '.cache/character-inputs.json'


def digest(path):
    with open(path, 'rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def save_texture(source, destination, size, data=False, alpha=None):
    image = source if isinstance(source, bpy.types.Image) else bpy.data.images.load(str(source), check_existing=False)
    image.colorspace_settings.name = 'Non-Color' if data else 'sRGB'
    width, height = image.size
    if max(width, height) > size:
        image.scale(round(width * size / max(width, height)), round(height * size / max(width, height)))
    if alpha is not None:
        mask = alpha if isinstance(alpha, bpy.types.Image) else bpy.data.images.load(str(alpha), check_existing=False)
        mask.colorspace_settings.name = 'Non-Color'
        mask.scale(*image.size)
        pixels = np.array(image.pixels[:], dtype=np.float32).reshape(-1, 4)
        pixels[:, 3] = np.array(mask.pixels[:], dtype=np.float32).reshape(-1, 4)[:, 0]
        image.pixels.foreach_set(pixels.ravel())
    # Several supplied normals are float/16-bit images. Runtime GPU compression
    # uses eight-bit input: quantize explicitly instead of committing huge floats.
    if image.depth > 32 or alpha is not None:
        byte_image = bpy.data.images.new('Eight-bit runtime map', width=image.size[0], height=image.size[1],
            alpha=alpha is not None or image.depth in (64, 128), float_buffer=False, is_data=data)
        byte_image.colorspace_settings.name = image.colorspace_settings.name
        byte_image.pixels.foreach_set(image.pixels[:])
        image = byte_image
    image.file_format = 'PNG'
    image.filepath_raw = str(destination)
    image.save()


def packed_surface(metal, rough, destination, size, default_metal=0, default_rough=.65):
    channels = []
    for source in (metal, rough):
        if source is None:
            channels.append(None)
            continue
        image = source if isinstance(source, bpy.types.Image) else bpy.data.images.load(str(source), check_existing=False)
        image.colorspace_settings.name = 'Non-Color'
        image.scale(size, size)
        channels.append(np.array(image.pixels[:], dtype=np.float32).reshape(-1, 4)[:, 0])
    pixels = np.ones((size * size, 4), dtype=np.float32)
    pixels[:, 0] = default_metal if channels[0] is None else channels[0]
    pixels[:, 1:3] = 0
    pixels[:, 3] = 1 - (default_rough if channels[1] is None else channels[1])
    image = bpy.data.images.new('Packed metal and smoothness', width=size, height=size, alpha=True)
    image.colorspace_settings.name = 'Non-Color'
    image.pixels.foreach_set(pixels.ravel())
    image.file_format = 'PNG'
    image.filepath_raw = str(destination)
    image.save()
    bpy.data.images.remove(image)


def bounds(meshes):
    points = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    return Vector([min(p[i] for p in points) for i in range(3)]), Vector([max(p[i] for p in points) for i in range(3)])


def simple_rig(meshes, key):
    # These two original FBX downloads contain no bones or skin weights. Supply an
    # explicitly authored game skeleton and smooth distance weights; no cloud service.
    mn, mx = bounds(meshes)
    center = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    size = 2 / (mx.z - mn.z)
    for obj in meshes:
        obj.data.transform(Matrix.Scale(size, 4) @ Matrix.Translation(-center) @ obj.matrix_world)
        obj.matrix_world = Matrix.Identity(4)
    armature = bpy.data.armatures.new('Seraphim game skeleton')
    rig = bpy.data.objects.new('GameRig', armature)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    def bone(name, start, end, parent=None):
        b = armature.edit_bones.new(name)
        b.head, b.tail = start, end
        if parent:
            b.parent = armature.edit_bones[parent]
        return b
    bone('Hips', (0, 0, .98), (0, 0, 1.16))
    bone('Spine', (0, 0, 1.16), (0, 0, 1.35), 'Hips')
    bone('Chest', (0, 0, 1.35), (0, 0, 1.52), 'Spine')
    bone('Neck', (0, 0, 1.52), (0, 0, 1.65), 'Chest')
    bone('Head', (0, 0, 1.65), (0, 0, 1.88), 'Neck')
    all_vertices = np.array([tuple(v.co) for o in meshes for v in o.data.vertices])
    for side, sign in (('L', 1), ('R', -1)):
        def center_at(z, fallback):
            mask = (all_vertices[:, 0] * sign > .07) & (abs(all_vertices[:, 2] - z) < .05)
            sample = all_vertices[mask]
            return tuple(np.median(sample, axis=0)) if len(sample) else fallback
        knee = list(center_at(.53, (sign * .17, 0, .53)))
        ankle = list(center_at(.11, (sign * .2, 0, .11)))
        knee[2], ankle[2] = .53, .11
        bone('UpperLeg.' + side, (sign * .13, 0, .98), knee, 'Hips')
        bone('LowerLeg.' + side, knee, ankle, 'UpperLeg.' + side)
        bone('Foot.' + side, ankle, (ankle[0], ankle[1] - .16, .055), 'LowerLeg.' + side)
        shoulder = (sign * .23, 0, 1.49)
        elbow = (sign * .37, -.015, 1.22)
        hand = (sign * (.49 if key == 'ash-invoker' else .4), -.04, .97)
        bone('UpperArm.' + side, shoulder, elbow, 'Chest')
        bone('Forearm.' + side, elbow, hand, 'UpperArm.' + side)
        bone('Hand.' + side, hand, (hand[0] + sign * .035, -.06, .85), 'Forearm.' + side)
    bpy.ops.object.mode_set(mode='OBJECT')
    segments = [(b.name, np.array(b.head_local), np.array(b.tail_local)) for b in rig.data.bones]
    for obj in meshes:
        groups = {name: obj.vertex_groups.new(name=name) for name, _, _ in segments}
        for vertex in obj.data.vertices:
            p = np.array(vertex.co)
            distances = []
            for name, a, b in segments:
                delta = b - a
                nearest = a + delta * np.clip(np.dot(p - a, delta) / np.dot(delta, delta), 0, 1)
                distance = np.linalg.norm(p - nearest)
                # Prevent arms borrowing torso weights and skirts splitting at the knee.
                if ('Arm' in name or 'Hand' in name) and abs(p[0]) < .24:
                    distance += .35
                if 'Leg' in name and p[2] > 1.02:
                    distance += .45
                distances.append((distance, name))
            nearest = sorted(distances)[:3]
            weights = np.array([1 / (d + .035) ** 5 for d, _ in nearest])
            weights /= weights.sum()
            for (_, name), weight in zip(nearest, weights):
                groups[name].add([vertex.index], float(weight), 'REPLACE')
        obj.parent = rig
        modifier = obj.modifiers.new('Game skin', 'ARMATURE')
        modifier.object = rig
    return rig


def prepare(spec):
    key, source = spec['key'], Path(spec['source'])
    folder = ASSETS / key
    folder.mkdir(parents=True, exist_ok=True)
    texture_dir = source.parent.parent / 'textures'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if source.suffix == '.blend':
        bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False, use_scripts=False)
    elif source.suffix == '.glb':
        bpy.ops.import_scene.gltf(filepath=str(source))
    else:
        bpy.ops.import_scene.fbx(filepath=str(source), use_anim=False, ignore_leaf_bones=True)
    for obj in list(bpy.data.objects):
        if obj.type not in {'MESH', 'ARMATURE'} or obj.name.startswith('Bone Shape'):
            bpy.data.objects.remove(obj, do_unlink=True)
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    for rig in rigs:
        rig.animation_data_clear()
        rig.data.pose_position = 'REST'
    if not rigs:
        rigs = [simple_rig(meshes, key)]
    bpy.context.view_layer.update()
    # Remove authoring-only modifiers while preserving the skin. Decimation happens
    # against the rest mesh, and never gets evaluated on an animated surface.
    for obj in meshes:
        obj.hide_set(False)
        obj.hide_render = False
        for modifier in list(obj.modifiers):
            if modifier.type != 'ARMATURE':
                obj.modifiers.remove(modifier)
        if obj.data.shape_keys:
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.shape_key_remove(all=True)
    material_specs = []
    used_materials = list({slot.material.name: slot.material for obj in meshes for slot in obj.material_slots if slot.material}.values())
    def texture(prefix, kind):
        matches = sorted(texture_dir.glob(prefix + '_' + kind + '.*'))
        return matches[0] if matches else None
    def image(name):
        result = bpy.data.images.get(name)
        if result is None:
            raise RuntimeError('Missing packed source image: ' + name)
        return result
    for material in used_materials:
        original = material.name
        clean = re.sub(r'[^a-zA-Z0-9_-]', '_', original)
        material.name = clean
        base = normal = metallic = rough = emission = alpha = None
        alpha_clip, double_sided = False, False
        size = 2048 if key in ('fallen-angel', 'infernal-wyrm') else 1024
        if key == 'fallen-angel':
            prefix = 'Eyes' if original.startswith('Eyes') else original
            if original == 'Scarf': prefix = 'T_Scarf'
            base, normal = texture(prefix, 'BaseColor'), texture(prefix, 'Normal')
            metallic, rough = texture(prefix, 'Metallic'), texture(prefix, 'Roughness')
            if original == 'Hair':
                base, normal = texture_dir / 'HairBase.png', texture_dir / 'Normal.png'
                alpha_clip = double_sided = True
                size = 1024
            if original in ('Scarf', 'Skirt'):
                double_sided = True
        elif key == 'cinder-reaver':
            # The nested original ZIP has the same diffuse as the outer texture copy.
            base = source.parent / 'devil.png'
        elif key == 'ash-invoker':
            part = 'Body' if original == '02 - Default' else 'Hair' if original == '03 - Default' else 'Armor'
            prefix = 'LD_DemonWitch_' + part
            base, normal = texture(prefix, 'BaseColor'), texture(prefix, 'Normal')
            metallic, rough = texture(prefix, 'Metallic'), texture(prefix, 'Roughness')
            emission = texture(prefix, 'Emissive')
            double_sided = part == 'Hair'
            if part == 'Body': size = 2048
        elif key == 'obsidian-brute':
            base, normal = texture(original, 'Diffuse'), texture(original, 'Normal')
        elif key == 'infernal-wyrm':
            base = texture_dir / 'Baked_BaseColor_2.png'
            normal = texture_dir / 'normal_1.png'
            metallic = texture_dir / 'Baked_MetallicRoughness_3@channels=B.png'
            rough = texture_dir / 'Baked_MetallicRoughness_3@channels=G.png'
            emission = texture_dir / 'Baked_Emit_0.png'
        else:
            prefix = {'Scourger Material': 'Scourger', 'Eye Material': 'Scourger', 'Attire': 'Scourger Attire', 'Feather': 'Feather', 'Axe': 'Axe'}[original]
            base = image(prefix + ' Diffuse.png')
            normal = image('Main Feather Normal.png' if original == 'Feather' else prefix + ' Normal.png')
            rough = bpy.data.images.get(prefix + ' Roughness.png')
            if original == 'Attire':
                metallic = image('Scourger Attire Metalness1.png')
                alpha = image('Scourger Attire Alpha.exr')
            if original in ('Scourger Material', 'Eye Material'):
                alpha = image('Scourger Alpha.png')
            if original == 'Feather': alpha = image('Feather Alpha.png')
            alpha_clip = alpha is not None
            double_sided = original in ('Attire', 'Feather')
        if base is None:
            raise RuntimeError(f'Missing diffuse texture for {key}/{original}')
        entry = {'name': clean, 'alphaClip': alpha_clip, 'doubleSided': double_sided}
        for prop, texture_source, data in (('baseColor', base, False), ('normal', normal, True), ('emission', emission, False)):
            if texture_source is not None:
                filename = f'{clean}_{prop}.png'
                save_texture(texture_source, folder / filename, size, data, alpha if prop == 'baseColor' else None)
                entry[prop] = filename
        filename = f'{clean}_metalSmooth.png'
        packed_surface(metallic, rough, folder / filename, min(size, 1024), .12 if key == 'cinder-reaver' else 0)
        entry['metalSmooth'] = filename
        material_specs.append(entry)
    lod = key != 'fallen-angel'
    for obj in list(meshes):
        if obj.type != 'MESH': continue
        if key in ('ash-invoker', 'rift-scourger') and len(obj.data.polygons) > 2500:
            bpy.context.view_layer.objects.active = obj
            decimate = obj.modifiers.new('Runtime detail', 'DECIMATE')
            decimate.ratio = .65 if key == 'rift-scourger' else .6
            bpy.ops.object.modifier_apply(modifier=decimate.name)
        if lod:
            obj.name += '_LOD0'
            reduced = obj.copy()
            reduced.data = obj.data.copy()
            reduced.name = obj.name[:-1] + '1'
            bpy.context.collection.objects.link(reduced)
            bpy.context.view_layer.objects.active = reduced
            decimate = reduced.modifiers.new('Distant detail', 'DECIMATE')
            decimate.ratio = .38 if key == 'obsidian-brute' else .28
            bpy.ops.object.modifier_apply(modifier=decimate.name)
    bpy.ops.object.select_all(action='SELECT')
    for rig in rigs:
        for pose in rig.pose.bones:
            for constraint in list(pose.constraints):
                pose.constraints.remove(constraint)
        rig.data.pose_position = 'POSE'
        for pose in rig.pose.bones: pose.matrix_basis = Matrix.Identity(4)
    filename = 'FallenAngel.fbx' if key == 'fallen-angel' else key + '.fbx'
    bpy.context.view_layer.objects.active = rigs[0]
    bpy.ops.export_scene.fbx(filepath=str(folder / filename), use_selection=True,
        object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, bake_anim=False,
        mesh_smooth_type='FACE', use_mesh_modifiers=False, path_mode='STRIP',
        axis_forward='-Z', axis_up='Y', armature_nodetype='NULL')
    triangles = {level: sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bpy.data.objects if o.type == 'MESH' and (not lod or o.name.endswith('_LOD' + str(level)))) for level in (0, 1)}
    if not lod: triangles[1] = 0
    definition = {'key': key, 'model': filename, 'lod': lod, 'triangles': triangles, 'materials': material_specs}
    (folder / 'model.json').write_text(json.dumps(definition, indent=2) + '\n', encoding='utf-8', newline='\n')
    print('SERAPHIM_PREPARED ' + key + ' ' + json.dumps(triangles), flush=True)


for spec in json.loads(INPUTS.read_text(encoding='utf-8-sig')):
    if '--only' in sys.argv and spec['key'] != sys.argv[sys.argv.index('--only') + 1]:
        continue
    prepare(spec)
