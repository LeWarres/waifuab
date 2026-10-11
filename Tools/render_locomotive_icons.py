"""Render transparent roster thumbnails from the existing Blender sources."""
import bpy, os
from mathutils import Vector
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
for source, mesh_name, output in (("locomotive.blend","Locomotive","loco_classic"),("locomotive_redwhite.blend","Locomotive_RedWhite","loco_redwhite")):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT,"Tools",source))
    for ob in list(bpy.data.objects):
        if ob.name!=mesh_name: bpy.data.objects.remove(ob,do_unlink=True)
    scene=bpy.context.scene
    scene.render.engine="BLENDER_EEVEE"
    scene.render.film_transparent=True
    scene.render.resolution_x=512;scene.render.resolution_y=320;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG";scene.render.image_settings.color_mode="RGBA"
    scene.view_settings.view_transform="Standard"
    world=bpy.data.worlds.new("IconWorld");world.use_nodes=True
    world.node_tree.nodes["Background"].inputs[0].default_value=(.6,.7,.9,1)
    scene.world=world
    light=bpy.data.objects.new("IconLight",bpy.data.lights.new("IconLight","AREA"));scene.collection.objects.link(light)
    light.location=(2,-4,7);light.data.energy=1700;light.data.size=5
    light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat("-Z","Y").to_euler()
    camera=bpy.data.objects.new("IconCamera",bpy.data.cameras.new("IconCamera"));scene.collection.objects.link(camera)
    camera.location=(6,-6,4);camera.rotation_euler=(Vector((0,0,1.1))-camera.location).to_track_quat("-Z","Y").to_euler()
    camera.data.type="ORTHO";camera.data.ortho_scale=6.5;scene.camera=camera
    scene.render.filepath=os.path.join(ROOT,"Assets","Resources","Menu",output+".png")
    bpy.ops.render.render(write_still=True)
