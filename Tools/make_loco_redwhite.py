"""New low-poly diesel locomotive in the existing game's scale and visual language."""
import os
import sys
import math
import bpy
import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import lowpoly as lp

ROOT = lp.ROOT
SCALE = 5.0 / 8.4
lp.reset()
lp.parts.clear()

def color(name, srgb, metal=0.0, emit=0.0):
    linear = tuple(c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4 for c in srgb)
    return lp.mat(name, linear, rough=.78, metal=metal, emit=emit)

RED = color("RW_Red", (.83, .055, .075))
RED_DARK = color("RW_DeepRed", (.51, .025, .045))
WHITE = color("RW_White", (.94, .95, .97))
BLACK = color("RW_Black", (.055, .065, .080))
STEEL = color("RW_Steel", (.32, .36, .41), .25)
GLASS = color("RW_Glass", (.24, .57, .70))
LAMP = color("RW_Lamp", (1.0, .96, .82), emit=1.0)

box, prism, cyl, both = lp.box, lp.prism, lp.cyl, lp.both

def rod(name, a, b, radius, material, sides=8):
    a, b = Vector(a), Vector(b)
    ob = cyl((a + b) / 2, radius, (b - a).length, "Z", material, sides, name)
    ob.rotation_euler = (b - a).to_track_quat("Z", "Y").to_euler()
    return ob

# The front is +X, with the same rail height and width as the existing locomotive.
box(-4.02, 4.0, -1.20, 1.20, .64, .86, BLACK, "Chassis")
box(-1.13, 1.02, -.91, .91, .16, .61, BLACK, "FuelTank")
both(lambda a,b: box(-1.00, .85, a,b, .28,.54, STEEL, "TankBand"), .91,.94)
for bx in (-2.55, 2.40):
    box(bx-1.20, bx+1.20, -1.03, 1.03, .29,.58, BLACK, "Bogie")
    for ax in (-.78,0,.78):
        x = bx + ax
        cyl((x,0,.36), .36, 2.25, "Y", STEEL, 14, "Wheel")
        for side in (-1,1):
            cyl((x,side*1.145,.36), .22,.055, "Y", BLACK, 12, "WheelInset")
            cyl((x,side*1.18,.36), .09,.07, "Y", STEEL, 8, "AxleCap")
    both(lambda a,b: box(bx-1.15,bx+1.15,a,b,.35,.46,BLACK,"BogieBeam"),1.19,1.24)
    for x in (bx-.43,bx+.43):
        both(lambda a,b: box(x-.14,x+.14,a,b,.45,.56,STEEL,"Suspension"),1.20,1.25)
box(-4.35,-4.00,-.22,.22,.49,.72,BLACK,"RearCoupler")

# Engine hood: red panels, white waist stripe and chamfered shoulders.
prism([(-3.92,.86),(.45,.86),(.45,2.99),(-3.92,2.99)],-1.10,1.10,RED,"EngineHood")
prism([(-3.92,2.99),(.45,2.99),(.45,3.23),(-3.92,3.23)],-.79,.79,BLACK,"HoodRoof")
for side in (-1,1):
    y0,y1 = sorted((side*.79,side*1.10))
    # Roof shoulder slopes downward away from the centre.
    verts = [(-3.92,side*.79,3.23),(.45,side*.79,3.23),(.45,side*1.10,2.99),(-3.92,side*1.10,2.99)]
    lp.mesh("RoofShoulder",verts,[(0,1,2,3)],RED_DARK)
both(lambda a,b: box(-3.93,.47,a,b,1.18,1.40,WHITE,"WhiteWaistStripe"),1.10,1.125)
both(lambda a,b: box(-3.93,.47,a,b,.88,1.00,RED_DARK,"LowerSkirt"),1.10,1.13)
box(-3.95,-3.925,-1.10,1.10,1.18,1.40,WHITE,"RearWhiteStripe")
box(-3.955,-3.93,-.70,.70,2.08,2.73,BLACK,"RearGrille")
for z in (2.16,2.26,2.36,2.46,2.56,2.66):
    box(-3.97,-3.955,-.64,.64,z,z+.025,STEEL,"RearGrilleSlat")
for y in (-.86,.86):
    cyl((-3.97,y,1.62),.10,.055,"X",BLACK,10,"RearMarkerHousing")
    cyl((-4.01,y,1.62),.063,.035,"X",RED,10,"RearMarker")
def side_panels(a,b):
    for i in range(5):
        x = -3.72 + i*.79
        box(x,x+.65,a,b,1.58,2.75,RED_DARK,"EnginePanelSeam")
        aa,bb = (a-.025,b) if a<0 else (a,b+.025)
        box(x+.035,x+.615,aa,bb,1.62,2.70,RED,"EngineDoor")
        for j in range(6):
            z=2.21+j*.065
            aaa,bbb=(aa-.022,bb) if a<0 else (aa,bb+.022)
            box(x+.10,x+.55,aaa,bbb,z,z+.026,BLACK,"CoolingLouver")
        box(x+.51,x+.54,aaa,bbb,1.77,1.91,STEEL,"DoorHandle")
    rod("SideHandrail",(-3.82,(a+b)/2,1.47),(.36,(a+b)/2,1.47),.025,BLACK)
both(side_panels,1.145,1.16)

# Two polygonal cooling fans and a small paired exhaust stack.
for x in (-3.03,-1.70):
    cyl((x,0,3.26),.52,.09,"Z",BLACK,16,"FanHousing")
    cyl((x,0,3.315),.435,.025,"Z",STEEL,16,"FanGrille")
    for i in range(8):
        a=i*math.pi/4
        rod("FanSpoke",(x,0,3.334),(x+.40*math.cos(a),.40*math.sin(a),3.334),.019,BLACK)
    cyl((x,0,3.35),.11,.045,"Z",BLACK,10,"FanHub")
for y in (-.27,.27):
    cyl((-.35,y,3.39),.11,.33,"Z",BLACK,8,"Exhaust")
    cyl((-.35,y,3.56),.145,.055,"Z",BLACK,8,"ExhaustCap")

# New cab silhouette: white upper cab and broad black split windscreen.
cab=[(.45,.86),(2.24,.86),(2.24,2.27),(1.88,3.48),(.45,3.48)]
prism(cab,-1.24,1.24,WHITE,"WhiteCab")
both(lambda a,b: prism([(.45,.86),(2.24,.86),(2.24,2.04),(.45,1.38)],a,b,RED,"CabRedSweep"),1.24,1.265)
box(.37,2.02,-1.32,1.32,3.48,3.62,BLACK,"CabRoof")
both(lambda a,b: box(.73,1.75,a,b,2.42,3.20,BLACK,"SideWindowFrame"),1.24,1.28)
both(lambda a,b: box(.81,1.67,a,b,2.50,3.12,GLASS,"SideWindow"),1.28,1.295)
both(lambda a,b: box(1.22,1.255,a,b,2.49,3.13,BLACK,"SideWindowDivider"),1.295,1.305)
both(lambda a,b: box(.54,.59,a,b,1.49,2.33,BLACK,"CabDoorSeam"),1.266,1.275)
both(lambda a,b: box(.65,.80,a,b,1.85,1.89,BLACK,"CabDoorHandle"),1.27,1.295)
def slope(z,out):
    return 2.24-.36*(z-2.27)/1.21+out*.958,z+out*.285
def pane(a,b,z0,z1,out,material,name):
    x0,h0=slope(z0,out);x1,h1=slope(z1,out)
    lp.mesh(name,[(x0,a,h0),(x0,b,h0),(x1,b,h1),(x1,a,h1)],[(0,1,2,3)],material)
for a,b in ((-1.14,-.065),(.065,1.14)):
    pane(a,b,2.44,3.32,.020,BLACK,"FrontWindowFrame")
    pane(a+.075,b-.075,2.52,3.24,.041,GLASS,"FrontWindow")
    y=(a+b)/2
    pane(y-.022,y+.022,2.53,3.06,.061,BLACK,"Wiper")
box(1.53,1.97,-.45,.45,3.62,3.90,BLACK,"RoofHeadlightBox")
for y in (-.23,.23):
    cyl((1.99,y,3.77),.102,.045,"X",LAMP,12,"RoofHeadlight")

# Short angular nose, a white chevron face, and black utility details.
prism([(2.24,.86),(3.53,.86),(3.53,1.91),(3.27,2.21),(2.24,2.21)],-1.00,1.00,RED,"RedNose")
box(2.20,3.29,-1.035,1.035,2.21,2.29,WHITE,"NoseTop")
box(3.531,3.55,-.88,.88,1.01,1.91,BLACK,"HeadlightPanel")
lp.mesh("WhiteFrontChevron",[(3.562,-.87,1.87),(3.562,0,1.56),(3.562,.87,1.87),(3.562,.87,1.65),(3.562,0,1.34),(3.562,-.87,1.65)],[(0,1,4,5),(1,2,3,4)],WHITE)
for y in (-.64,.64):
    cyl((3.59,y,1.19),.15,.04,"X",STEEL,12,"HeadlightRim")
    cyl((3.62,y,1.19),.108,.045,"X",LAMP,12,"Headlight")
for y in (-.19,.19):
    cyl((2.78,y,2.38),.10,.30,"X",BLACK,8,"AirHorn")
both(lambda a,b: box(2.41,3.34,a,b,1.11,1.32,WHITE,"NoseSideStripe"),1.00,1.025)

# White handrail footings and black rails/steps, retaining the heavy front pilot.
box(3.53,4.08,-1.20,1.20,.86,.95,BLACK,"FrontDeck")
prism([(3.82,.12),(4.50,.12),(4.19,.93),(3.70,.93)],-1.23,1.23,BLACK,"Pilot")
box(4.24,4.34,-.33,.33,.39,.67,STEEL,"CouplerSocket")
box(4.34,4.62,-.19,.19,.47,.63,BLACK,"FrontCoupler")
for y in (-.97,.97):
    cyl((4.22,y,.84),.062,.045,"X",RED,10,"MarkerLight")
def rails(a,b):
    y=(a+b)/2
    for x in (2.38,3.26,4.02):
        box(x-.065,x+.065,a-.025,b+.025,.96,1.09,WHITE,"RailFoot")
        rod("RailPost",(x,y,1.06),(x,y,1.86),.033,BLACK)
    rod("Handrail",(2.38,y,1.86),(4.02,y,1.86),.033,BLACK)
    rod("FrontHandrail",(4.02,y,1.86),(4.02,math.copysign(.46,y),1.86),.033,BLACK)
    for i in range(3):
        box(2.55,3.22,a-.02,b+.02,.22+i*.20,.28+i*.20,BLACK,"CabStep")
both(rails,1.16,1.23)

# Single mesh in game units, standard FBX orientation, no gameplay references changed.
bpy.ops.object.select_all(action="DESELECT")
for ob in lp.parts: ob.select_set(True)
bpy.context.view_layer.objects.active=lp.parts[0]
bpy.ops.object.join()
loco=bpy.context.object
loco.name="Locomotive_RedWhite"
loco.scale=(SCALE,)*3
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(loco.data)
bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
bm.to_mesh(loco.data);bm.free()
bpy.ops.object.shade_flat()
folder=os.path.join(ROOT,"Assets","Resources","Models")
bpy.ops.export_scene.fbx(filepath=os.path.join(folder,"Locomotive_RedWhite.fbx"),use_selection=True,
    apply_scale_options="FBX_SCALE_ALL",axis_forward="-Z",axis_up="Y",bake_space_transform=True,
    object_types={"MESH"},mesh_smooth_type="FACE",bake_anim=False)
print("LOCOMOTIVE",sum(len(p.vertices)-2 for p in loco.data.polygons),"TRIS",tuple(loco.dimensions))

# Same simple studio presentation as the original locomotive.
scene=bpy.context.scene
scene.render.engine="BLENDER_EEVEE"
scene.render.resolution_x=1400;scene.render.resolution_y=900
scene.render.resolution_percentage=100
scene.view_settings.view_transform="Standard"
ground=color("StudioGround",(.38,.42,.48))
bpy.ops.mesh.primitive_plane_add(size=60,location=(0,0,-.01))
bpy.context.object.data.materials.append(ground)
world=bpy.data.worlds.new("StudioSky");world.use_nodes=True
world.node_tree.nodes["Background"].inputs[0].default_value=(.34,.44,.60,1)
world.node_tree.nodes["Background"].inputs[1].default_value=.7
scene.world=world
for name,pos,power,size in (("Key",(1,-5,7),1500,5),("Fill",(4,3,5),1000,4)):
    light=bpy.data.objects.new(name,bpy.data.lights.new(name,"AREA"))
    scene.collection.objects.link(light);light.location=pos
    light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat("-Z","Y").to_euler()
    light.data.energy=power;light.data.shape="DISK";light.data.size=size
cam=bpy.data.objects.new("Camera",bpy.data.cameras.new("Camera"));scene.collection.objects.link(cam)
scene.camera=cam;cam.data.type="ORTHO";cam.data.ortho_scale=7.3
target=Vector((.05,0,1.02))
shots=(("front",(6.5,-6.5,4.1)),("back",(-6,5,3.8)))
cam.location=shots[0][1];cam.rotation_euler=(target-cam.location).to_track_quat("-Z","Y").to_euler()
bpy.ops.object.select_all(action="DESELECT");loco.select_set(True);bpy.context.view_layer.objects.active=loco
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,"Tools","locomotive_redwhite.blend"))
for name,pos in shots:
    cam.location=pos;cam.rotation_euler=(target-cam.location).to_track_quat("-Z","Y").to_euler()
    scene.render.filepath=os.path.join(ROOT,"Tools","loco_redwhite_"+name+".png")
    bpy.ops.render.render(write_still=True)
