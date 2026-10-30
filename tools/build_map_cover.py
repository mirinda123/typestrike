"""Build bullet and movement collision maps. The original game image is only needed for an optional preview."""
import argparse
import gzip
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
FOOT_HALF_WIDTH = 10
FOOT_HALF_HEIGHT = 5


def bullet_mask(data):
    size = data['width'], data['height']
    mask = Image.new('1', size)
    draw = ImageDraw.Draw(mask)
    for shape in data['polygons']:
        draw.polygon([tuple(p) for p in shape['points']], fill=1)
    for trunk in data['trunks']:
        draw.rectangle(tuple(trunk['box']), fill=1)
    for canopy in data['canopies']:
        draw.ellipse(tuple(canopy['box']), fill=1)
    return mask


def movement_mask(data, cover):
    # Water and raised flowerbeds block feet without changing bullet cover.
    movement = cover.copy()
    movement_draw = ImageDraw.Draw(movement)
    for shape in data['movement_only']:
        movement_draw.polygon([tuple(p) for p in shape['points']], fill=1)
    return expand_for_feet(movement)


def expand_for_feet(mask):
    """A character center must leave space for its whole foot rectangle."""
    grown = mask.convert('L')
    for axis, radius in [(0, FOOT_HALF_WIDTH), (1, FOOT_HALF_HEIGHT)]:
        source = grown.copy()
        for shift in range(-radius, radius + 1):
            shifted = Image.new('L', mask.size)
            offset = (shift, 0) if axis == 0 else (0, shift)
            shifted.paste(source, offset)
            grown = ImageChops.lighter(grown, shifted)
    return grown.convert('1')


def write_mask(path, mask):
    # Each scanline, most significant bit first. A fixed timestamp makes the
    # compressed resource reproducible and easy to compare before packaging.
    path.write_bytes(gzip.compress(mask.tobytes(), mtime=0))


def write_preview(data, mask, map_path, output):
    original = Image.open(map_path).convert('RGBA')
    assert original.size == mask.size
    tint = Image.new('RGBA', mask.size, (255, 64, 32, 0))
    tint.putalpha(mask.convert('L').point(lambda value: 96 if value else 0))
    preview = Image.alpha_composite(original, tint)
    terrain = Image.new('RGBA', mask.size)
    terrain_draw = ImageDraw.Draw(terrain)
    for shape in data['movement_only']:
        points = [tuple(point) for point in shape['points']]
        if shape['kind'] == 'flowerbed':
            fill, edge = (80, 255, 80, 88), (80, 255, 80)
        else:
            fill, edge = (20, 180, 255, 88), (50, 220, 255)
        terrain_draw.polygon(points, fill=fill)
        terrain_draw.line(points + points[:1], fill=edge, width=2)
    preview = Image.alpha_composite(preview, terrain)
    pen = ImageDraw.Draw(preview)
    for shape in data['polygons']:
        pen.line([tuple(point) for point in shape['points'] + shape['points'][:1]],
                 fill=(255, 72, 30), width=2)
    for trunk in data['trunks']:
        pen.rectangle(tuple(trunk['box']), outline=(255, 225, 0), width=2)
    for canopy in data['canopies']:
        pen.ellipse(tuple(canopy['box']), outline=(255, 225, 0), width=2)
    output.parent.mkdir(parents=True, exist_ok=True)
    preview.convert('RGB').save(output)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--map', type=Path, help='locally extracted POLICE_BACKGROUND PNG')
    parser.add_argument('--preview', type=Path, help='write a map overlay outside the release')
    args = parser.parse_args()
    if args.preview and not args.map:
        parser.error('--preview requires --map')

    data = json.loads((ROOT / 'assets/MapCover.json').read_text(encoding='utf-8'))
    cover = bullet_mask(data)
    output = ROOT / 'assets/MapCover.gz'
    write_mask(output, cover)
    write_mask(ROOT / 'assets/MovementMap.gz', movement_mask(data, cover))
    print(f'{len(data["polygons"])} buildings/vehicles, {len(data["trunks"])} trunks; '
          f'{len(data["canopies"])} canopies; {len(data["movement_only"])} movement-only regions; '
          f'{len(cover.tobytes())} mask bytes -> {output.stat().st_size} compressed bytes')
    if args.preview:
        write_preview(data, cover, args.map, args.preview)


if __name__ == '__main__':
    main()
