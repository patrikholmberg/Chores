"""Rasterise the app icon (white check mark on green) to PNG without third-party libraries."""
import struct, sys, zlib, math

BG = (47, 125, 91)
FG = (255, 255, 255)

def dist_to_segment(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(px - (ax + t * dx), py - (ay + t * dy))

def render(size, content_scale, rounded):
    # Check mark points in unit space, scaled into the centred content box.
    pts = [(0.22, 0.52), (0.42, 0.72), (0.78, 0.30)]
    off = (1 - content_scale) / 2
    p = [((off + x * content_scale) * size, (off + y * content_scale) * size) for x, y in pts]
    half_width = 0.075 * content_scale * size
    radius = 0.22 * size if rounded else 0
    rows = []
    for y in range(size):
        row = bytearray([0])
        for x in range(size):
            cx, cy = x + 0.5, y + 0.5
            alpha = 255
            if radius:
                nx = min(max(cx, radius), size - radius)
                ny = min(max(cy, radius), size - radius)
                d = math.hypot(cx - nx, cy - ny)
                alpha = int(255 * max(0.0, min(1.0, radius - d + 0.5)))
            d = min(dist_to_segment(cx, cy, *p[0], *p[1]), dist_to_segment(cx, cy, *p[1], *p[2]))
            cover = max(0.0, min(1.0, half_width - d + 0.5))
            r, g, b = (round(BG[i] + (FG[i] - BG[i]) * cover) for i in range(3))
            row += bytes((r, g, b, alpha))
        rows.append(bytes(row))
    raw = b"".join(rows)

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    return (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw, 9))
            + chunk(b"IEND", b""))

out = sys.argv[1]
for name, size, scale, rounded in [
    ("pwa-192x192.png", 192, 1.0, True),
    ("pwa-512x512.png", 512, 1.0, True),
    ("maskable-512x512.png", 512, 0.7, False),
    ("apple-touch-icon-180x180.png", 180, 0.85, False),
]:
    with open(f"{out}/{name}", "wb") as f:
        f.write(render(size, scale, rounded))
    print(name)
