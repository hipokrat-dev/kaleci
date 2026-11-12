"""Run: Blender --background --python Blender/build_goal.py -- /absolute/project/path
Original procedural geometry, metre units, no external assets.
"""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1])
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'
bpy.context.scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('Goal_Ivory'); mat.diffuse_color=(.86,.89,.85,1)
def rod(name,a,b,radius):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius,depth=(b-a).length,location=(a+b)/2)
    ob=bpy.context.object; ob.name=name
    ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); ob.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
# Blender Z up; exported FBX converts to Unity Y up.
for x in [-3.72,3.72]: rod('Post', (x,0,0),(x,0,2.5),.06)
rod('Crossbar',(-3.72,0,2.5),(3.72,0,2.5),.06)
for i in range(35):
    x=-3.66+i*7.32/34
    rod('NetVertical',(x,-1.8,0),(x,-1.8,2.44),.006)
    rod('NetRoof',(x,-1.8,2.44),(x,0,2.44),.006)
for i in range(13):
    z=i*2.44/12
    rod('NetHorizontal',(-3.66,-1.8,z),(3.66,-1.8,z),.006)
    for x in [-3.66,3.66]: rod('NetSide',(x,-1.8,z),(x,0,z),.006)
# Merge the net to reduce renderers; keep posts separate for collisions.
bpy.ops.object.select_all(action='DESELECT')
for ob in bpy.context.scene.objects:
    if ob.name.startswith('Net'): ob.select_set(True); bpy.context.view_layer.objects.active=ob
bpy.ops.object.join(); bpy.context.object.name='Net_Static_LowCost'
for ob in bpy.context.scene.objects:
    bpy.context.view_layer.objects.active=ob; ob.select_set(True)
    if ob.type=='MESH':
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(); bpy.ops.object.mode_set(mode='OBJECT')
    ob.select_set(False)
(root/'Assets/Penalty/Art').mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Blender/Goal.blend'))
bpy.ops.export_scene.fbx(filepath=str(root/'Assets/Penalty/Art/Goal.fbx'),axis_forward='-Z',axis_up='Y',apply_unit_scale=True,object_types={'MESH'},bake_anim=False)
print('PENALTY_BLENDER_EXPORT_OK')
