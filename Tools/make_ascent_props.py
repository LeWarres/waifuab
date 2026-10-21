"""Blender: authored orbital debris, beveled silhouettes and readable mechanical details.
Run: blender -b --python Tools/make_ascent_props.py
"""
import bpy, bmesh, math, os
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Ascent', 'Models')
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
COLORS = {
    'HullIvory': (.79, .83, .85), 'HullShadow': (.38, .47, .58),
    'OrbitalNavy': (.085, .14, .25), 'SolarBlue': (.13, .32, .57),
    'SolarTeal': (.23, .56, .66), 'FoilGold': (.78, .59, .27),
    'SafetyCoral': (.78, .29, .25), 'CableRubber': (.10, .13, .18),
    'GlowTelemetry': (.23, .72, .82), 'ExposedSteel': (.55, .62, .69),
}
M = {}
for name, color in COLORS.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get('Principled BSDF')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Roughness'].default_value = .83
    M[name] = mat

parts = []
def finish_part(ob, name, material, bevel=0):
    ob.name = name
    ob.data.materials.append(M[material])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new('MachinedEdge', 'BEVEL')
        mod.width, mod.segments = bevel, 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = ob.modifiers.new('WeightedNormals', 'WEIGHTED_NORMAL')
        mod.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

def box(name, at, size, material, bevel=.035, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at, rotation=tuple(math.radians(a) for a in rot))
    ob = bpy.context.object
    ob.scale = size
    return finish_part(ob, name, material, bevel)

def cylinder(name, at, radius, depth, material, axis='Z', top=None):
    rotation = (math.pi/2, 0, 0) if axis == 'Y' else (0, math.pi/2, 0) if axis == 'X' else (0, 0, 0)
    bpy.ops.mesh.primitive_cone_add(vertices=20, radius1=radius, radius2=radius if top is None else top, depth=depth, location=at, rotation=rotation)
    return finish_part(bpy.context.object, name, material, .018)

def rod(name, a, b, radius, material):
    delta = Vector(b) - Vector(a)
    ob = cylinder(name, (Vector(a) + Vector(b)) / 2, radius, delta.length, material)
    ob.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return ob

def bolts(x0, x1, z0, z1, y):
    for x in (x0, x1):
        for z in (z0, z1):
            cylinder('Fastener', (x, y, z), .038, .035, 'ExposedSteel', 'Y')

def dish(center, radius):
    # A shallow concave reflector, with a dark back, thick lip and a suspended receiver.
    verts, faces = [(0, .12, 0)], []
    rings, segments = 5, 28
    for ring in range(1, rings + 1):
        r = radius * ring/rings
        for i in range(segments):
            a = 2*math.pi*i/segments
            verts.append((r*math.cos(a), .12 - .32*(r/radius)**2, r*math.sin(a)))
    for i in range(segments): faces.append((0, 1+i, 1+(i+1)%segments))
    for ring in range(rings-1):
        for i in range(segments):
            a, b = 1+ring*segments+i, 1+ring*segments+(i+1)%segments
            faces.append((a, a+segments, b+segments, b))
    mesh = bpy.data.meshes.new('ReflectorMesh')
    mesh.from_pydata(verts, [], faces)
    ob = bpy.data.objects.new('Reflector', mesh)
    bpy.context.collection.objects.link(ob)
    ob.location = center
    ob.data.materials.append(M['HullIvory'])
    parts.append(ob)
    c = Vector(center)
    for a in (0, 2.094, 4.189):
        rod('FeedStrut', c+Vector((radius*.9*math.cos(a), -.18, radius*.9*math.sin(a))), c+Vector((0, -.55, 0)), .022, 'ExposedSteel')
    cylinder('Receiver', c+Vector((0, -.53, 0)), .1, .22, 'FoilGold', 'Y')
    bpy.ops.mesh.primitive_torus_add(major_segments=28, minor_segments=6, major_radius=radius, minor_radius=.028, location=c+Vector((0, -.2, 0)), rotation=(math.pi/2,0,0))
    finish_part(bpy.context.object, 'ReflectorRim', 'ExposedSteel')

def export(name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in parts: ob.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(ob.data); bm.free()
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name+'.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', bake_space_transform=True, apply_scale_options='FBX_SCALE_ALL', mesh_smooth_type='FACE', add_leaf_bones=False)
    print(name, 'triangles', sum(len(p.vertices)-2 for p in ob.data.polygons))
    bpy.data.objects.remove(ob, do_unlink=True)
    parts.clear()

def solar_wing(side, damaged=False):
    x = side*1.55
    box('SolarFrame', (x,0,0), (1.7,.14,1.65), 'ExposedSteel')
    box('PanelBed', (x,-.085,0), (1.55,.055,1.48), 'OrbitalNavy', .015)
    for col in range(4):
        for row in range(5):
            if damaged and col == 3 and row > 2: continue
            xx, zz = x-.57+col*.38, -.59+row*.295
            box('PhotovoltaicCell', (xx,-.122,zz), (.335,.028,.25), 'SolarTeal' if (row+col)%5==0 else 'SolarBlue', .012)
            box('CellBus', (xx,-.142,zz), (.012,.008,.22), 'HullShadow', .002)
    box('WingHinge', (side*.68,0,0), (.42,.3,.3), 'FoilGold')

# Communications satellite: restrained ivory/gold bus, gridded arrays, dish, radiators, engines.
box('SatelliteBus', (0,.06,0), (1,.88,1.5), 'FoilGold', .12)
box('AvionicsCover', (0,-.43,0), (.86,.12,1.12), 'HullIvory', .065)
bolts(-.34,.34,-.45,.45,-.51)
for z in (-.25,-.12,.01,.14): box('RadiatorSlat', (0,-.515,z), (.48,.025,.045), 'OrbitalNavy', .006)
for side in (-1,1):
    solar_wing(side)
    cylinder('Thruster', (side*.34,0,-.88), .17,.32,'HullShadow',top=.11)
    cylinder('ThrusterMouth',(side*.34,0,-1.05),.12,.025,'OrbitalNavy')
rod('AntennaMast',(0,0,.75),(.15,0,1.2),.035,'ExposedSteel')
dish((.15,-.18,1.15), .46)
box('Telemetry',(.3,-.515,.37),(.12,.03,.11),'GlowTelemetry',.015)
export('OrbitalSatellite')

# Re-entry canister: octagonal pressure vessel, locking bands, service panel and stencilled stripe.
cylinder('PressureShell',(0,0,0),.65,1.65,'HullIvory')
for z in (-.65,.65):
    cylinder('ReinforcedBand',(0,0,z),.69,.18,'HullShadow')
    cylinder('BandTrim',(0,0,z+.07),.7,.035,'FoilGold')
cylinder('UpperCap',(0,0,.88),.55,.18,'HullShadow',top=.42)
cylinder('Valve',(0,0,1.02),.16,.14,'FoilGold')
box('AccessPanel',(0,-.63,0),(.66,.12,.64),'OrbitalNavy',.04)
box('WarningPlate',(0,-.705,.08),(.5,.025,.2),'SafetyCoral',.01)
for x in (-.16,0,.16): box('WarningSlash',(x,-.724,.08),(.06,.012,.18),'HullIvory',.004,rot=(0,-20,0))
bolts(-.25,.25,-.23,.23,-.72)
for x in (-.27,.27): rod('CarryHandle',(x,-.05,.97),(x,-.05,1.23),.055,'ExposedSteel')
rod('CarryGrip',(-.27,-.05,1.23),(.27,-.05,1.23),.055,'ExposedSteel')
export('PressureCanister')

# Broken hull: recognizable torn spacecraft skin with structural ribs, pipes and severed cable ends.
box('HullFragment',(0,.08,0),(1.6,.3,1.25),'HullShadow',.11,rot=(0,-12,0))
box('CeramicPanel',(-.26,-.14,.1),(1.05,.16,1.1),'HullIvory',.07,rot=(0,-12,0))
box('PaintStripe',(-.28,-.235,.23),(1,.022,.16),'SafetyCoral',.015,rot=(0,-12,0))
box('ExposedRib',(.5,-.15,-.13),(.18,.22,1.75),'ExposedSteel',.025,rot=(0,22,0))
for z in (-.48,.12,.56):
    rod('CrossRib',(-.7,.1,z),(.87,.1,z+.1),.055,'FoilGold')
for i in range(3):
    x=.65+i*.15
    rod('SeveredCable',(x,-.1,.42),(x+.2,-.25,.75),.04,'CableRubber')
    rod('CopperEnd',(x+.2,-.25,.75),(x+.24,-.26,.86),.026,'FoilGold')
cylinder('BrokenCoupler',(-.5,0,-.77),.22,.3,'HullShadow','X')
bolts(-.61,.15,-.32,.47,-.25)
export('HullWreckage')

# Booster section with three nozzles and a torn solar array: a second large silhouette.
cylinder('EngineHousing',(0,0,0),.57,1.3,'HullIvory')
cylinder('EngineCollar',(0,0,-.48),.64,.18,'HullShadow')
for x in (-.34,0,.34):
    cylinder('Bell',(x,0,-.92),.22,.55,'ExposedSteel',top=.12)
    cylinder('BellInterior',(x,0,-1.205),.17,.02,'OrbitalNavy')
solar_wing(1,True)
rod('FuelLine',(-.48,-.4,-.35),(-.48,-.4,.62),.05,'FoilGold')
box('ServicePlate',(0,-.55,.2),(.5,.08,.4),'SafetyCoral',.035)
for i in range(4): box('CoolingFin',(0,-.6,-.22+i*.1),(.56,.06,.035),'HullShadow',.008)
export('DamagedBooster')

# Compact propulsion cradle mounted behind the existing locomotive, not a replacement train.
box('DriveCradle',(0,0,.18),(1.45,.72,.4),'HullShadow',.08)
for x in (-.45,.45):
    cylinder('DriveHousing',(x,0,0),.28,.5,'HullIvory')
    cylinder('ExhaustRim',(x,0,-.27),.31,.13,'FoilGold')
    cylinder('ExhaustInner',(x,0,-.345),.23,.025,'OrbitalNavy')
    box('DriveIndicator',(x,-.29,.1),(.19,.025,.08),'GlowTelemetry',.01)
export('FlightDrive')
