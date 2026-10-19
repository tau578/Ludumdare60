"""Rasterize HandCursorGraphic's polygons into transparent 64px cursor textures.

Requires Pillow. Run from any directory; geometry and detail strokes are read
from the archived C# geometry beside this script, outside the runtime assets.
"""
from pathlib import Path
import json
import math
import re
import uuid

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Tools/Art/HandCursorGeometry.cs.txt"
OUTPUT = ROOT / "Assets/Sprites/Cursors"
SCALE = 8
SIZE = 64


def export():
    source = SOURCE.read_text(encoding="utf-8-sig")
    shapes = {}
    for name, body in re.findall(r"Vector2\[\] (\w+) = \{(.*?)\};", source, re.S):
        shapes[name] = [(float(x), float(y)) for x, y in
                        re.findall(r"new Vector2\(([\d.]+),\s*([\d.]+)\)", body)]
    strokes = re.compile(
        r"Line\(mesh, new Vector2\(([\d.]+),\s*([\d.]+)\), "
        r"new Vector2\(([\d.]+),\s*([\d.]+)\), ([\d.]+)f, outline\);")
    populate = source.split("protected override void OnPopulateMesh", 1)[1].split("private Vector2 Local", 1)[0]
    page_block, hand_blocks = populate.split("Fill(mesh, shape, triangles, new Vector2(2, 3), shadow);", 1)
    turn_block, rest = hand_blocks.split("else if (grabbing)", 1)
    grab_block, point_block = rest.split("else", 1)
    outline = (30, 35, 29, 255)
    shadow = (10, 15, 12, 110)
    glove = (238, 235, 214, 255)
    OUTPUT.mkdir(parents=True, exist_ok=True)

    def meta(path, folder=False):
        target = Path(str(path) + ".meta")
        if target.exists():
            return
        header = f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n"
        if folder:
            body = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        else:
            body = """TextureImporter:
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  maxTextureSize: 64
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 7
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 64
    textureFormat: -1
    textureCompression: 0
    overridden: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        target.write_text(header + body, encoding="utf-8")

    meta(OUTPUT, folder=True)
    manifest = []
    for name, shape_name, detail, hotspot, flip in [
        ("cursor_pointing", "Pointing", point_block, (20, 2), False),
        ("cursor_grab", "Grab", grab_block, (29, 28), False),
        ("cursor_page_right", "Pinch", turn_block, (22, 23), False),
        ("cursor_page_left", "Pinch", turn_block, (22, 23), True),
    ]:
        canvas = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE))

        def polygon(points, color):
            layer = Image.new("RGBA", canvas.size)
            # Pixel centres are offset by half a pixel from mesh coordinates.
            ImageDraw.Draw(layer).polygon([(round(x * SCALE - .5), round(y * SCALE - .5))
                                          for x, y in points], fill=color)
            canvas.alpha_composite(layer)

        def line(a, b, width):
            dx, dy = b[0] - a[0], b[1] - a[1]
            length = math.hypot(dx, dy)
            nx, ny = -dy / length * width / 2, dx / length * width / 2
            polygon([(a[0]-nx, a[1]-ny), (a[0]+nx, a[1]+ny),
                     (b[0]+nx, b[1]+ny), (b[0]-nx, b[1]-ny)], outline)

        def silhouette(points, fill):
            polygon([(x + 2, y + 3) for x, y in points], shadow)
            polygon(points, fill)
            for a, b in zip(points, points[1:] + points[:1]):
                line(a, b, 2.3)

        def details(block):
            for values in strokes.findall(block):
                x1, y1, x2, y2, width = map(float, values)
                line((x1, y1), (x2, y2), width)

        if shape_name == "Pinch":
            silhouette(shapes["Page"], (211, 199, 165, 255))
            details(page_block)
        silhouette(shapes[shape_name], glove)
        details(detail)
        # BOX resolves subpixel coverage without adding ringing beyond the shape.
        result = canvas.resize((SIZE, SIZE), Image.Resampling.BOX)
        if flip:
            result = result.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            hotspot = (SIZE - hotspot[0], hotspot[1])
        path = OUTPUT / f"{name}.png"
        result.save(path)
        meta(path)
        manifest.append({"file": path.relative_to(ROOT).as_posix(), "hotspot": hotspot})
        assert result.mode == "RGBA" and result.size == (64, 64)
        assert result.getchannel("A").getextrema() == (0, 255)
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    export()
