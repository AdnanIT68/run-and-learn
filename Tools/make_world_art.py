"""Scenery for every word world, written to Assets/Resources/Worlds.

Per world (World1..World8):
  far.png  2048x1024 sky and distant shapes, horizontally seamless
  mid.png  2048x1024 transparent buildings, trees and props, seamless
Shared:
  Obstacles/<world>_<kind>.png  Twemoji skins for spike, crate and saw obstacles
  Ambient/<name>.png            petals, fluff, leaves, birds, rain, snow, planes and fireflies
  Ground/<set>/terrain_*.png    Kenney terrain tiles copied from Assets/Art/Tiles

The ground line sits at 73% of the image height, where the run's ground tiles
start, so everything below it is hidden behind the track.

Usage: Tools/.venv/Scripts/python.exe -I Tools/make_world_art.py
"""

import math
import random
import shutil
import urllib.request
from io import BytesIO
from pathlib import Path

import resvg_py
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Worlds"
TILES = ROOT / "Assets" / "Art" / "Tiles"
TWEMOJI_SVG = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@15.1.0/assets/svg/{code}.svg"

W, H = 2048, 1024
S = 2  # supersampling: draw at twice the size, scale down for smooth edges
GROUND = int(H * 0.73)


# ---------------------------------------------------------------- helpers

def canvas(transparent: bool, fill=(0, 0, 0)) -> Image.Image:
    return Image.new("RGBA", (W * S, H * S), (fill + (0,)) if transparent else fill + (255,))


def finish(image: Image.Image) -> Image.Image:
    return image.resize((W, H), Image.LANCZOS)


def sky(image: Image.Image, top, bottom):
    draw = ImageDraw.Draw(image)
    for y in range(H * S):
        t = y / (H * S)
        draw.line([(0, y), (W * S, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))


def wrap(fn, x, *args, **kwargs):
    """Draws a shape at x and again one tile away, so the image tiles seamlessly."""
    for offset in (-W, 0, W):
        fn(x + offset, *args, **kwargs)


def p(*values):
    return [v * S for v in values]


class Painter:
    def __init__(self, image: Image.Image):
        self.image = image
        self.d = ImageDraw.Draw(image)

    def ellipse(self, cx, cy, rx, ry, colour):
        self.d.ellipse(p(cx - rx, cy - ry, cx + rx, cy + ry), fill=colour)

    def dome(self, cx, base, rx, ry, colour):
        """Upper half of an ellipse standing on y = base: a real dome, not a ball."""
        self.d.pieslice(p(cx - rx, base - ry, cx + rx, base + ry), 180, 360, fill=colour)

    def rect(self, x0, y0, x1, y1, colour, radius=0):
        if radius:
            self.d.rounded_rectangle(p(x0, y0, x1, y1), radius * S, fill=colour)
        else:
            self.d.rectangle(p(x0, y0, x1, y1), fill=colour)

    def poly(self, points, colour):
        self.d.polygon([(x * S, y * S) for x, y in points], fill=colour)

    def line(self, points, colour, width):
        self.d.line([(x * S, y * S) for x, y in points], fill=colour, width=width * S)

    def hills(self, base, height, colour, count, seed, width_range=(260, 520)):
        rng = random.Random(seed)
        for i in range(count):
            cx = i * W / count + rng.uniform(-60, 60)
            rx = rng.uniform(*width_range)
            ry = height * rng.uniform(0.75, 1.15)
            wrap(lambda x: self.ellipse(x, base, rx, ry, colour), cx)
        self.rect(0, base, W, H, colour)

    def cloud(self, cx, cy, scale, colour=(255, 255, 255, 235)):
        def one(x):
            for dx, dy, r in ((-60, 10, 42), (-15, -15, 55), (40, 0, 46), (0, 18, 50)):
                self.ellipse(x + dx * scale, cy + dy * scale, r * scale * 1.2, r * scale, colour)
        wrap(one, cx)

    def tree(self, x, base, h, leaf, trunk=(120, 80, 50, 255), fruit=None, rng=None):
        def one(px):
            self.rect(px - h * 0.07, base - h * 0.45, px + h * 0.07, base, trunk)
            self.ellipse(px, base - h * 0.62, h * 0.36, h * 0.34, leaf)
            self.ellipse(px - h * 0.2, base - h * 0.5, h * 0.24, h * 0.22, leaf)
            self.ellipse(px + h * 0.2, base - h * 0.52, h * 0.24, h * 0.22, leaf)
            if fruit and rng:
                local = random.Random(int(x))
                for _ in range(7):
                    fx = px + local.uniform(-0.3, 0.3) * h
                    fy = base - h * local.uniform(0.45, 0.8)
                    self.ellipse(fx, fy, h * 0.035, h * 0.035, fruit)
        wrap(one, x)

    def spruce(self, x, base, h, leaf, snow=None):
        def one(px):
            self.rect(px - h * 0.05, base - h * 0.15, px + h * 0.05, base, (100, 70, 45, 255))
            for i, w in enumerate((0.42, 0.34, 0.25)):
                top = base - h * (0.35 + i * 0.25) - h * 0.25
                bottom = base - h * (0.12 + i * 0.25)
                self.poly([(px, top), (px - h * w, bottom), (px + h * w, bottom)], leaf)
                if snow:
                    self.poly([(px, top), (px - h * w * 0.45, top + (bottom - top) * 0.45),
                               (px + h * w * 0.45, top + (bottom - top) * 0.45)], snow)
        wrap(one, x)

    def house(self, x, base, w, h, wall, roof, window=(120, 180, 230, 255), chimney=True, door=(130, 85, 55, 255)):
        def one(px):
            self.rect(px - w / 2, base - h, px + w / 2, base, wall)
            self.poly([(px - w * 0.62, base - h), (px, base - h - w * 0.45), (px + w * 0.62, base - h)], roof)
            if chimney:
                self.rect(px + w * 0.18, base - h - w * 0.38, px + w * 0.3, base - h - w * 0.12, (150, 90, 70, 255))
            for wx in (-0.27, 0.27):
                self.rect(px + w * wx - w * 0.1, base - h * 0.72, px + w * wx + w * 0.1, base - h * 0.45, window, 4)
            self.rect(px - w * 0.09, base - h * 0.38, px + w * 0.09, base, door, 4)
        wrap(one, x)

    def minaret(self, x, base, h, colour=(245, 242, 235, 255), cap=(70, 110, 100, 255)):
        def one(px):
            w = h * 0.075
            self.rect(px - w, base - h, px + w, base, colour)
            self.rect(px - w * 1.5, base - h * 0.78, px + w * 1.5, base - h * 0.74, colour)
            self.poly([(px - w * 1.1, base - h), (px, base - h - h * 0.22), (px + w * 1.1, base - h)], cap)
        wrap(one, x)

    def mosque(self, x, base, w):
        def one(px):
            self.rect(px - w / 2, base - w * 0.45, px + w / 2, base, (238, 232, 220, 255))
            self.rect(px - w * 0.36, base - w * 0.55, px + w * 0.36, base - w * 0.45, (225, 218, 205, 255))
            self.dome(px, base - w * 0.55, w * 0.33, w * 0.3, (90, 125, 115, 255))
            self.rect(px - w * 0.012, base - w * 0.97, px + w * 0.012, base - w * 0.84, (200, 170, 70, 255))
            self.rect(px - w * 0.09, base - w * 0.25, px + w * 0.09, base, (120, 80, 55, 255), 6)
            for ax in (-0.33, 0.33):
                self.ellipse(px + w * ax, base - w * 0.28, w * 0.05, w * 0.08, (110, 160, 200, 255))
        wrap(one, x)
        self.minaret(x + w * 0.62, base, w * 1.35)

    def sebilj(self, x, base, w):
        def one(px):
            wood = (150, 95, 55, 255)
            self.rect(px - w / 2, base - w * 0.55, px + w / 2, base - w * 0.08, wood)
            for ax in (-0.3, 0.0, 0.3):
                self.rect(px + w * ax - w * 0.06, base - w * 0.5, px + w * ax + w * 0.06, base - w * 0.15,
                          (230, 215, 180, 255), 3)
            self.rect(px - w * 0.62, base - w * 0.1, px + w * 0.62, base, (215, 205, 190, 255))
            self.dome(px, base - w * 0.55, w * 0.55, w * 0.42, (80, 120, 95, 255))
            self.rect(px - w * 0.03, base - w * 1.05, px + w * 0.03, base - w * 0.9, (200, 170, 70, 255))
        wrap(one, x)

    def shop(self, x, base, w, h):
        def one(px):
            self.rect(px - w / 2, base - h, px + w / 2, base, (170, 115, 70, 255))
            self.poly([(px - w * 0.62, base - h), (px - w * 0.45, base - h - h * 0.45),
                       (px + w * 0.45, base - h - h * 0.45), (px + w * 0.62, base - h)], (190, 85, 55, 255))
            self.rect(px - w * 0.4, base - h * 0.7, px + w * 0.4, base - h * 0.25, (95, 60, 35, 255), 4)
            stripes = 6
            for i in range(stripes):
                colour = (235, 80, 70, 255) if i % 2 == 0 else (250, 245, 235, 255)
                sx = px - w * 0.48 + i * w * 0.96 / stripes
                self.poly([(sx, base - h * 0.78), (sx + w * 0.96 / stripes, base - h * 0.78),
                           (sx + w * 0.96 / stripes + 6, base - h * 0.64), (sx + 6, base - h * 0.64)], colour)
        wrap(one, x)

    def fence(self, y, colour, gap=46, height=70):
        for i in range(int(W / gap) + 1):
            px = i * gap
            self.poly([(px, y - height), (px + 10, y - height - 14), (px + 20, y - height), (px + 20, y), (px, y)], colour)
        self.rect(0, y - height * 0.75, W, y - height * 0.62, colour)
        self.rect(0, y - height * 0.32, W, y - height * 0.19, colour)


# ---------------------------------------------------------------- worlds

def garden():
    far = canvas(False)
    sky(far, (120, 200, 255), (210, 240, 255))
    f = Painter(far)
    f.ellipse(1750, 140, 70, 70, (255, 221, 87, 255))
    for cx, cy, s in ((300, 160, 1.0), (1000, 110, 0.8), (1450, 230, 0.7)):
        f.cloud(cx, cy, s)
    f.hills(GROUND - 90, 150, (160, 220, 130, 255), 5, 1)
    for i in range(12):
        f.tree(80 + i * 170, GROUND - 60, 120, (110, 185, 95, 255))
    f.hills(GROUND, 70, (120, 200, 105, 255), 6, 2)

    mid = canvas(True)
    m = Painter(mid)
    rng = random.Random(3)
    for x in (180, 820, 1450):
        m.tree(x, GROUND + 10, 300, (90, 170, 80, 255), fruit=(225, 50, 60, 255), rng=rng)
    m.fence(GROUND + 12, (250, 250, 245, 255))
    for i in range(16):
        x = 60 + i * 128
        stem = GROUND + 12
        m.line([(x, stem), (x, stem - 120)], (80, 150, 70, 255), 5)
        petal = (255, 205, 60, 255) if i % 2 == 0 else (240, 120, 160, 255)
        for a in range(8):
            ang = a * math.pi / 4
            m.ellipse(x + math.cos(ang) * 18, stem - 120 + math.sin(ang) * 18, 12, 12, petal)
        m.ellipse(x, stem - 120, 11, 11, (140, 90, 40, 255))
    return far, mid


def farm():
    far = canvas(False)
    sky(far, (125, 195, 245), (235, 240, 225))
    f = Painter(far)
    f.ellipse(320, 150, 70, 70, (255, 215, 90, 255))
    for cx, cy, s in ((900, 140, 0.9), (1600, 190, 0.8)):
        f.cloud(cx, cy, s)
    f.hills(GROUND - 80, 140, (225, 195, 110, 255), 4, 4, (380, 640))
    f.hills(GROUND - 20, 90, (150, 200, 105, 255), 5, 5)
    for x in (400, 1300):
        f.house(x, GROUND - 40, 70, 50, (190, 60, 50, 255), (120, 40, 35, 255), chimney=False)

    mid = canvas(True)
    m = Painter(mid)

    def barn(x):
        red, white = (185, 50, 42, 255), (245, 240, 230, 255)
        m.rect(x - 170, GROUND - 220, x + 170, GROUND + 10, red)
        m.poly([(x - 200, GROUND - 220), (x, GROUND - 360), (x + 200, GROUND - 220)], (120, 40, 35, 255))
        m.rect(x - 70, GROUND - 120, x + 70, GROUND + 10, white)
        m.rect(x - 60, GROUND - 112, x + 60, GROUND + 10, red)
        m.line([(x - 60, GROUND - 112), (x + 60, GROUND + 10)], white, 8)
        m.line([(x + 60, GROUND - 112), (x - 60, GROUND + 10)], white, 8)
        m.rect(x - 40, GROUND - 300, x + 40, GROUND - 250, white)
    wrap(barn, 500)

    def silo(x):
        m.rect(x - 55, GROUND - 300, x + 55, GROUND + 10, (200, 200, 205, 255))
        m.ellipse(x, GROUND - 300, 55, 40, (150, 155, 165, 255))
    wrap(silo, 760)

    for x in (1100, 1230, 1700):
        wrap(lambda px: (m.ellipse(px, GROUND - 40, 75, 60, (235, 195, 90, 255)),
                         m.line([(px - 60, GROUND - 40), (px + 60, GROUND - 40)], (200, 160, 70, 255), 5)), x)
    m.fence(GROUND + 12, (150, 100, 60, 255), gap=90, height=60)
    return far, mid


def home():
    far = canvas(False)
    sky(far, (135, 190, 250), (232, 238, 250))
    f = Painter(far)
    f.ellipse(1600, 150, 65, 65, (255, 220, 95, 255))
    for cx, cy, s in ((250, 170, 0.9), (1100, 120, 0.8)):
        f.cloud(cx, cy, s)
    f.hills(GROUND - 70, 170, (140, 200, 120, 255), 4, 6, (360, 600))
    rng = random.Random(7)
    for i in range(22):
        x = rng.uniform(0, W)
        y = GROUND - 60 - rng.uniform(0, 110)
        f.house(x, y, 34, 26, (245, 240, 230, 255), (175, 85, 60, 255), chimney=False, window=(150, 190, 220, 255))
    f.hills(GROUND, 60, (110, 185, 100, 255), 6, 8)

    mid = canvas(True)
    m = Painter(mid)
    walls = ((250, 244, 232, 255), (245, 225, 190, 255), (235, 240, 245, 255), (250, 235, 215, 255))
    roofs = ((190, 70, 50, 255), (150, 80, 50, 255), (200, 95, 60, 255), (120, 70, 55, 255))
    for i, x in enumerate((180, 640, 1110, 1600)):
        m.house(x, GROUND + 10, 230, 190, walls[i % 4], roofs[i % 4])
        m.tree(x + 245, GROUND + 10, 210, (100, 175, 85, 255))
    m.rect(0, GROUND - 30, W, GROUND + 12, (95, 160, 80, 255), 18)
    return far, mid


def bazaar():
    far = canvas(False)
    sky(far, (140, 195, 250), (250, 232, 205))
    f = Painter(far)
    f.ellipse(1700, 170, 70, 70, (255, 214, 100, 255))
    f.cloud(500, 150, 0.9)
    f.hills(GROUND - 70, 210, (120, 165, 120, 255), 3, 9, (520, 820))
    rng = random.Random(10)
    for i in range(40):
        x = rng.uniform(0, W)
        y = GROUND - 40 - rng.uniform(0, 150)
        f.house(x, y, 26, 20, (245, 240, 230, 255), (185, 90, 60, 255), chimney=False, window=(160, 190, 210, 255))
    for x in (300, 980, 1500):
        f.minaret(x, GROUND - 40, 170, (240, 238, 230, 255), (90, 120, 110, 255))
    f.rect(0, GROUND - 40, W, H, (200, 190, 175, 255))

    mid = canvas(True)
    m = Painter(mid)
    m.mosque(420, GROUND + 10, 300)
    m.sebilj(1150, GROUND + 10, 210)
    for x in (820, 1500, 1760):
        m.shop(x, GROUND + 10, 220, 170)
    return far, mid


def river():
    far = canvas(False)
    sky(far, (125, 195, 245), (220, 240, 255))
    f = Painter(far)
    f.ellipse(1750, 150, 65, 65, (255, 222, 100, 255))
    for cx, cy, s in ((350, 140, 0.9), (1150, 190, 0.7)):
        f.cloud(cx, cy, s)

    def cliff(x, colour, height):
        f.poly([(x - 340, GROUND), (x - 220, GROUND - height), (x - 60, GROUND - height - 60),
                (x + 120, GROUND - height + 10), (x + 340, GROUND)], colour)
    for x, h in ((200, 300), (900, 360), (1500, 280)):
        wrap(lambda px: cliff(px, (175, 180, 170, 255), h), x)
    f.hills(GROUND - 30, 110, (105, 170, 95, 255), 5, 11)
    f.rect(0, GROUND - 70, W, GROUND - 10, (70, 175, 185, 255))
    for i in range(20):
        x = i * 110
        f.line([(x, GROUND - 45), (x + 50, GROUND - 45)], (170, 225, 230, 255), 4)

    mid = canvas(True)
    m = Painter(mid)

    def bridge(x):
        stone = (225, 214, 192, 255)
        dark = (190, 178, 158, 255)
        span, rise = 460, 250
        top = GROUND - 70 - rise
        m.rect(x - span / 2 - 110, top - 40, x - span / 2, GROUND - 40, dark)
        m.rect(x + span / 2, top - 40, x + span / 2 + 110, GROUND - 40, dark)
        m.rect(x - span / 2 - 120, top - 120, x - span / 2 - 10, top - 40, stone)
        m.poly([(x - span / 2 - 130, top - 120), (x - span / 2 - 65, top - 175), (x - span / 2, top - 120)],
               (175, 95, 65, 255))
        arch_out = [(x + math.cos(a) * (span / 2 + 30), GROUND - 60 - math.sin(a) * (rise + 40))
                    for a in [i * math.pi / 30 for i in range(31)]]
        m.poly(arch_out + [(x - span / 2 - 30, GROUND - 40), (x + span / 2 + 30, GROUND - 40)], stone)
        arch_in = [(x + math.cos(a) * (span / 2 - 20), GROUND - 60 - math.sin(a) * (rise - 30))
                   for a in [i * math.pi / 30 for i in range(31)]]
        m.poly(arch_in, (0, 0, 0, 0))
    wrap(bridge, 1024)
    # Punch the arch opening back to transparent so the far layer's river shows through.
    for offset in (-W, 0, W):
        x = 1024 + offset
        hole = [(x + math.cos(a) * 210, GROUND - 60 - math.sin(a) * 220) for a in [i * math.pi / 30 for i in range(31)]]
        ImageDraw.Draw(mid).polygon([(px * S, py * S) for px, py in hole], fill=(0, 0, 0, 0))
    for x in (200, 1800):
        m.tree(x, GROUND + 10, 260, (95, 165, 85, 255))
    for x in (420, 1550, 1680):
        wrap(lambda px: m.ellipse(px, GROUND, 60, 40, (150, 150, 145, 255)), x)
    return far, mid


def mountain():
    far = canvas(False)
    sky(far, (165, 205, 240), (238, 246, 255))
    f = Painter(far)
    f.ellipse(1650, 140, 60, 60, (255, 240, 180, 255))

    def peak(x, h, colour, cap):
        f.poly([(x - h * 0.9, GROUND), (x, GROUND - h), (x + h * 0.9, GROUND)], colour)
        f.poly([(x - h * 0.25, GROUND - h * 0.72), (x, GROUND - h), (x + h * 0.25, GROUND - h * 0.72),
                (x + h * 0.08, GROUND - h * 0.66), (x - h * 0.08, GROUND - h * 0.7)], cap)
    for x, h in ((250, 480), (800, 560), (1350, 470), (1850, 520)):
        wrap(lambda px: peak(px, h, (140, 160, 190, 255), (250, 252, 255, 255)), x)
    f.hills(GROUND - 40, 90, (230, 238, 248, 255), 6, 12)
    for i in range(14):
        f.spruce(70 + i * 150, GROUND - 30, 120, (70, 120, 95, 255), (245, 250, 255, 255))

    mid = canvas(True)
    m = Painter(mid)
    for x in (150, 380, 1280, 1520, 1820):
        m.spruce(x, GROUND + 10, 320, (50, 105, 80, 255), (250, 252, 255, 255))

    def cabin(x):
        m.rect(x - 150, GROUND - 160, x + 150, GROUND + 10, (150, 95, 60, 255))
        for i in range(6):
            m.line([(x - 150, GROUND - 140 + i * 28), (x + 150, GROUND - 140 + i * 28)], (125, 78, 50, 255), 4)
        m.poly([(x - 190, GROUND - 160), (x, GROUND - 290), (x + 190, GROUND - 160)], (110, 70, 50, 255))
        m.poly([(x - 200, GROUND - 160), (x, GROUND - 300), (x + 200, GROUND - 160), (x + 170, GROUND - 175),
                (x, GROUND - 270), (x - 170, GROUND - 175)], (250, 252, 255, 255))
        m.rect(x - 45, GROUND - 120, x + 45, GROUND - 60, (255, 210, 120, 255), 6)
        for i in range(4):
            m.ellipse(x + 80 + i * 18, GROUND - 330 - i * 40, 20 + i * 6, 16 + i * 5, (230, 235, 240, 200))
        m.rect(x + 60, GROUND - 300, x + 100, GROUND - 230, (120, 80, 60, 255))
    wrap(cabin, 820)
    for x in (600, 1100, 1700):
        wrap(lambda px: m.ellipse(px, GROUND + 5, 110, 40, (248, 251, 255, 255)), x)
    return far, mid


def school():
    far = canvas(False)
    sky(far, (130, 195, 250), (225, 240, 255))
    f = Painter(far)
    f.ellipse(1700, 140, 65, 65, (255, 220, 95, 255))
    for cx, cy, s in ((300, 150, 0.9), (1100, 120, 0.8)):
        f.cloud(cx, cy, s)
    f.hills(GROUND - 60, 150, (150, 205, 125, 255), 4, 13, (380, 620))
    rng = random.Random(14)
    for i in range(14):
        x = rng.uniform(0, W)
        f.house(x, GROUND - 50 - rng.uniform(0, 70), 40, 30, (245, 240, 230, 255), (180, 90, 60, 255),
                chimney=False, window=(160, 195, 225, 255))
    f.hills(GROUND, 60, (115, 190, 100, 255), 6, 15)

    mid = canvas(True)
    m = Painter(mid)

    def building(x):
        wall, trim = (250, 205, 95, 255), (240, 240, 235, 255)
        m.rect(x - 330, GROUND - 330, x + 330, GROUND + 10, wall)
        m.poly([(x - 360, GROUND - 330), (x, GROUND - 430), (x + 360, GROUND - 330)], (175, 75, 55, 255))
        m.rect(x - 345, GROUND - 335, x + 345, GROUND - 318, trim)
        for row in range(2):
            for col in range(6):
                if col in (2, 3) and row == 1:
                    continue
                wx = x - 280 + col * 110
                wy = GROUND - 290 + row * 130
                m.rect(wx, wy, wx + 70, wy + 80, (140, 195, 235, 255), 6)
                m.line([(wx + 35, wy), (wx + 35, wy + 80)], trim, 4)
        m.rect(x - 70, GROUND - 130, x + 70, GROUND + 10, (150, 95, 60, 255), 8)
        m.ellipse(x, GROUND - 370, 38, 38, trim)
        m.line([(x, GROUND - 370), (x, GROUND - 395)], (60, 60, 70, 255), 5)
        m.line([(x, GROUND - 370), (x + 18, GROUND - 365)], (60, 60, 70, 255), 5)
    wrap(building, 620)

    def swing(x):
        frame = (220, 70, 60, 255)
        m.line([(x - 90, GROUND + 10), (x - 60, GROUND - 200)], frame, 10)
        m.line([(x + 90, GROUND + 10), (x + 60, GROUND - 200)], frame, 10)
        m.line([(x - 70, GROUND - 200), (x + 70, GROUND - 200)], frame, 10)
        m.line([(x - 20, GROUND - 200), (x - 20, GROUND - 60)], (90, 90, 95, 255), 3)
        m.line([(x + 20, GROUND - 200), (x + 20, GROUND - 60)], (90, 90, 95, 255), 3)
        m.rect(x - 32, GROUND - 62, x + 32, GROUND - 50, (60, 120, 200, 255), 3)
    wrap(swing, 1240)

    def slide(x):
        m.rect(x - 10, GROUND - 210, x + 10, GROUND + 10, (60, 120, 200, 255))
        m.rect(x - 60, GROUND - 215, x + 10, GROUND - 200, (60, 120, 200, 255))
        m.poly([(x + 10, GROUND - 210), (x + 200, GROUND + 5), (x + 175, GROUND + 10), (x + 5, GROUND - 185)],
               (250, 190, 60, 255))
    wrap(slide, 1480)
    for x in (1050, 1800):
        m.tree(x, GROUND + 10, 250, (100, 175, 85, 255))
    return far, mid


def night():
    far = canvas(False)
    sky(far, (18, 24, 66), (78, 66, 140))
    f = Painter(far)
    rng = random.Random(16)
    for _ in range(170):
        x, y = rng.uniform(0, W), rng.uniform(0, GROUND - 160)
        r = rng.choice((1.5, 2, 2, 3, 4))
        wrap(lambda px: f.ellipse(px, y, r, r, (255, 250, 220, rng.randint(150, 255))), x)
    # A few bigger twinkles with a soft cross.
    for x, y in ((420, 120), (980, 260), (1460, 90), (1880, 300)):
        def twinkle(px):
            f.line([(px - 16, y), (px + 16, y)], (255, 250, 220, 200), 2)
            f.line([(px, y - 16), (px, y + 16)], (255, 250, 220, 200), 2)
            f.ellipse(px, y, 5, 5, (255, 252, 230, 255))
        wrap(twinkle, x)
    # Crescent moon: a full disc with a sky-coloured disc bitten out of it.
    moon = Image.new("L", (W * S, H * S), 0)
    md = ImageDraw.Draw(moon)
    md.ellipse(p(1560, 90, 1720, 250), fill=255)
    md.ellipse(p(1603, 78, 1747, 222), fill=0)
    far.paste((255, 240, 185, 255), (0, 0), moon)
    f.hills(GROUND - 70, 150, (44, 46, 104, 255), 4, 17, (380, 620))

    # A small mosque on the far hill, its windows lit for the evening prayer.
    def far_mosque(px):
        base = GROUND - 150
        f.rect(px - 70, base - 45, px + 70, base + 20, (34, 36, 86, 255))
        f.dome(px, base - 45, 48, 44, (34, 36, 86, 255))
        f.rect(px + 85, base - 150, px + 97, base + 20, (34, 36, 86, 255))
        f.poly([(px + 82, base - 150), (px + 91, base - 182), (px + 100, base - 150)], (34, 36, 86, 255))
        for wx in (-40, 0, 40):
            f.rect(px + wx - 7, base - 25, px + wx + 7, base - 5, (255, 214, 120, 255), 3)
    wrap(far_mosque, 1320)
    for i in range(11):
        x = rng.uniform(0, W)
        base = GROUND - 30 - rng.uniform(0, 50)
        f.house(x, base, 46, 34, (52, 56, 112, 255), (40, 40, 92, 255),
                chimney=False, window=(255, 214, 120, 255), door=(40, 40, 90, 255))
    f.hills(GROUND, 60, (36, 40, 92, 255), 6, 18)

    mid = canvas(True)
    m = Painter(mid)

    def observatory(x):
        wall, dome = (88, 92, 150, 255), (150, 160, 205, 255)
        m.rect(x - 150, GROUND - 210, x + 150, GROUND + 10, wall)
        m.dome(x, GROUND - 210, 165, 150, dome)
        # The open slit in the dome with the telescope looking out of it.
        m.poly([(x - 18, GROUND - 210), (x - 30, GROUND - 350), (x + 30, GROUND - 350), (x + 18, GROUND - 210)],
               (30, 34, 80, 255))
        m.line([(x, GROUND - 250), (x + 70, GROUND - 400)], (210, 215, 235, 255), 22)
        m.rect(x - 45, GROUND - 100, x + 45, GROUND + 10, (60, 62, 115, 255), 8)
        m.rect(x - 110, GROUND - 160, x - 60, GROUND - 110, (255, 214, 120, 255), 6)
        m.rect(x + 60, GROUND - 160, x + 110, GROUND - 110, (255, 214, 120, 255), 6)
    wrap(observatory, 700)

    def lamp(x):
        m.rect(x - 6, GROUND - 230, x + 6, GROUND + 10, (40, 44, 80, 255))
        m.ellipse(x, GROUND - 240, 44, 44, (255, 200, 90, 70))
        m.ellipse(x, GROUND - 240, 20, 20, (255, 228, 150, 255))
    for x in (1180, 1700):
        wrap(lamp, x)
    for x in (150, 1420, 1900):
        m.tree(x, GROUND + 10, 270, (36, 58, 82, 255), trunk=(40, 38, 60, 255))
    for x in (380, 1000):
        wrap(lambda px: m.ellipse(px, GROUND + 5, 90, 36, (44, 70, 90, 255)), x)
    return far, mid


WORLDS = {
    "World1": garden,
    "World2": farm,
    "World3": home,
    "World4": bazaar,
    "World5": river,
    "World6": mountain,
    "World7": school,
    "World8": night,
}

# Obstacle skins. None of these pictures are words the child is learning.
OBSTACLES = {
    "World1": {"spike": "1f994", "crate": "1fab4", "saw": "1f40c"},   # hedgehog, potted plant, snail
    "World2": {"spike": "1fab5", "crate": "1f33e", "saw": "1f6de"},   # log, sheaf, wheel
    "World3": {"spike": "1f9f8", "crate": "1f4e6", "saw": "1f9f6"},   # teddy, box, yarn
    "World4": {"spike": "1f3fa", "crate": "1f9fa", "saw": "1f6d2"},   # amphora, basket, cart
    "World5": {"spike": "1faa8", "crate": "1fab5", "saw": "1f980"},   # rock, log, crab
    "World6": {"spike": "1f332", "crate": "1faa8", "saw": "1fab5"},   # spruce, rock, log
    "World7": {"spike": "1f9e9", "crate": "1fa91", "saw": "1fa80"},   # puzzle piece, chair, yo-yo
    "World8": {"spike": "1f525", "crate": "26fa", "saw": "1fab5"},    # campfire, tent, log
}

GROUND_SETS = ("dirt", "stone", "sand", "snow", "purple")
GROUND_PARTS = ("block_top", "block_center", "horizontal_left", "horizontal_middle", "horizontal_right")


def twemoji(code: str, size: int = 256) -> Image.Image:
    with urllib.request.urlopen(TWEMOJI_SVG.format(code=code), timeout=30) as response:
        svg = response.read().decode("utf-8")
    return Image.open(BytesIO(bytes(resvg_py.svg_to_bytes(svg_string=svg, width=size, height=size)))).convert("RGBA")


def ambient():
    """Small particle sprites, drawn rather than taken from vocabulary pictures."""
    shapes = {}

    petal = Image.new("RGBA", (64, 64), (255, 160, 200, 0))
    ImageDraw.Draw(petal).ellipse((14, 22, 50, 42), fill=(255, 160, 200, 255))
    shapes["petal"] = petal

    fluff = Image.new("RGBA", (64, 64), (255, 255, 255, 0))
    d = ImageDraw.Draw(fluff)
    for a in range(12):
        ang = a * math.pi / 6
        d.line((32, 32, 32 + math.cos(ang) * 24, 32 + math.sin(ang) * 24), fill=(255, 255, 255, 230), width=2)
    d.ellipse((28, 28, 36, 36), fill=(255, 255, 255, 255))
    shapes["fluff"] = fluff

    leaf = Image.new("RGBA", (64, 64), (230, 140, 50, 0))
    ImageDraw.Draw(leaf).polygon([(10, 32), (32, 14), (54, 32), (32, 50)], fill=(230, 140, 50, 255))
    shapes["leaf"] = leaf

    bird = Image.new("RGBA", (96, 64), (70, 80, 95, 0))
    ImageDraw.Draw(bird).line([(8, 22), (30, 40), (48, 30), (66, 40), (88, 22)], fill=(70, 80, 95, 255), width=7)
    shapes["bird"] = bird

    rain = Image.new("RGBA", (24, 64), (150, 200, 240, 0))
    ImageDraw.Draw(rain).line([(14, 4), (8, 60)], fill=(150, 200, 240, 230), width=4)
    shapes["rain"] = rain

    plane = Image.new("RGBA", (96, 64), (255, 255, 255, 0))
    pd = ImageDraw.Draw(plane)
    pd.polygon([(6, 34), (90, 10), (40, 40)], fill=(255, 255, 255, 255))
    pd.polygon([(40, 40), (90, 10), (52, 56)], fill=(215, 225, 240, 255))
    shapes["plane"] = plane

    spark = Image.new("RGBA", (48, 48), (255, 240, 140, 0))
    sd = ImageDraw.Draw(spark)
    sd.ellipse((4, 4, 44, 44), fill=(255, 240, 140, 90))
    sd.ellipse((16, 16, 32, 32), fill=(255, 250, 200, 255))
    shapes["spark"] = spark.filter(ImageFilter.GaussianBlur(3))

    flake = Image.new("RGBA", (48, 48), (255, 255, 255, 0))
    ImageDraw.Draw(flake).ellipse((10, 10, 38, 38), fill=(255, 255, 255, 255))
    shapes["snow"] = flake.filter(ImageFilter.GaussianBlur(2))
    return shapes


def main() -> None:
    for name, build in WORLDS.items():
        folder = OUT / name
        folder.mkdir(parents=True, exist_ok=True)
        far, mid = build()
        finish(far).convert("RGB").save(folder / "far.png")
        finish(mid).save(folder / "mid.png")

    obstacles = OUT / "Obstacles"
    obstacles.mkdir(parents=True, exist_ok=True)
    for world, kinds in OBSTACLES.items():
        for kind, code in kinds.items():
            target = obstacles / f"{world}_{kind}.png"
            if not target.exists():
                twemoji(code).save(target)

    ambient_dir = OUT / "Ambient"
    ambient_dir.mkdir(parents=True, exist_ok=True)
    for name, image in ambient().items():
        image.save(ambient_dir / f"{name}.png")

    for kind in GROUND_SETS:
        folder = OUT / "Ground" / kind
        folder.mkdir(parents=True, exist_ok=True)
        for part in GROUND_PARTS:
            shutil.copyfile(TILES / f"terrain_{kind}_{part}.png", folder / f"terrain_{kind}_{part}.png")

    print(f"scenery for {len(WORLDS)} worlds written to {OUT}")


if __name__ == "__main__":
    main()
