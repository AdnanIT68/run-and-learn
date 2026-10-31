"""Playable kids drawn in code, frame for frame matching Rukajja's animation.

Every kid gets the nine frames Rukajja's animator uses (Idle, Run_01..05,
Jump_Up, Fall_Air, Hit) at twice Rukajja's pixel size, facing right with the
feet on the bottom edge, plus a portrait for the character chooser.

Writes Assets/Resources/Characters/<id>/<frame>.png and <id>/portrait.png.

They are a team of young footballers (the owner's idea): every kid wears a kit
with a number, shorts down to the knee and socks pulled over it. Girls wear
long sleeves under the jersey and a longer jersey, some a sports hijab.
Bosnian names.

Usage: Tools/.venv/Scripts/python.exe -I Tools/make_characters.py [preview]
"""

import math
import sys
from dataclasses import dataclass, field
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Characters"

W, H = 282, 410          # twice Rukajja's 141x205 frames
S = 4                    # supersampling
INK = (58, 40, 52, 255)  # outline colour
LINE = 3.2               # outline width in frame pixels
FONT = "C:/Windows/Fonts/arialbd.ttf"


@dataclass
class Kid:
    """A young footballer: team kit, knee-long shorts and socks pulled over the knee."""
    id: str
    skin: tuple
    hair: tuple
    hair_style: str          # short, curly, braid, bun, hijab
    shirt: tuple             # jersey
    trim: tuple              # collar, cuffs, sock bands, number
    shorts: tuple
    socks: tuple
    boots: tuple
    number: int
    eyes: tuple = (95, 60, 40, 255)
    long_sleeves: tuple = None   # base layer colour under the jersey, None for bare forearms
    long_shirt: bool = False     # jersey reaching down over the shorts
    stripes: tuple = None        # vertical stripes on the jersey
    hijab: tuple = None
    glasses: bool = False
    gloves: tuple = None         # goalkeeper


WHITE = (250, 250, 250, 255)
NAVY = (35, 50, 95, 255)
BLACK = (45, 45, 55, 255)

KIDS = [
    # Bosnia and Herzegovina colours.
    Kid("emir", skin=(250, 214, 180, 255), hair=(110, 70, 40, 255), hair_style="short",
        shirt=(30, 80, 185, 255), trim=(250, 205, 40, 255), shorts=(30, 80, 185, 255),
        socks=(30, 80, 185, 255), boots=BLACK, number=10),
    Kid("amina", skin=(248, 210, 178, 255), hair=(80, 50, 30, 255), hair_style="hijab",
        hijab=NAVY, shirt=(110, 185, 240, 255), trim=WHITE, shorts=NAVY, socks=(110, 185, 240, 255),
        boots=(235, 90, 140, 255), number=7, long_sleeves=WHITE, long_shirt=True, eyes=(70, 120, 60, 255)),
    Kid("harun", skin=(230, 185, 145, 255), hair=(40, 30, 30, 255), hair_style="curly",
        shirt=(215, 45, 50, 255), trim=WHITE, shorts=WHITE, socks=(215, 45, 50, 255), boots=BLACK, number=9),
    Kid("lejla", skin=(250, 216, 186, 255), hair=(60, 35, 25, 255), hair_style="braid",
        shirt=(145, 90, 210, 255), trim=WHITE, shorts=(70, 50, 120, 255), socks=(145, 90, 210, 255),
        boots=(250, 200, 60, 255), number=8, long_sleeves=(70, 50, 120, 255), long_shirt=True,
        eyes=(60, 110, 160, 255)),
    # The goalkeeper: long sleeves and big gloves.
    Kid("tarik", skin=(245, 205, 168, 255), hair=(30, 25, 25, 255), hair_style="short",
        shirt=(250, 205, 40, 255), trim=BLACK, shorts=BLACK, socks=(250, 205, 40, 255), boots=BLACK,
        number=1, long_sleeves=(250, 205, 40, 255), gloves=(90, 200, 90, 255)),
    Kid("selma", skin=(240, 200, 165, 255), hair=(60, 40, 30, 255), hair_style="hijab",
        hijab=WHITE, shirt=(245, 130, 50, 255), trim=WHITE, shorts=BLACK, socks=(245, 130, 50, 255),
        boots=WHITE, number=11, long_sleeves=BLACK, long_shirt=True),
    Kid("kenan", skin=(252, 220, 190, 255), hair=(230, 180, 90, 255), hair_style="short", glasses=True,
        shirt=WHITE, trim=BLACK, shorts=BLACK, socks=WHITE, boots=(70, 140, 220, 255), number=5,
        stripes=BLACK, eyes=(70, 120, 180, 255)),
    Kid("hana", skin=(246, 208, 172, 255), hair=(140, 80, 45, 255), hair_style="bun",
        shirt=(30, 170, 160, 255), trim=WHITE, shorts=WHITE, socks=(30, 170, 160, 255),
        boots=(240, 100, 110, 255), number=6, long_sleeves=WHITE, long_shirt=True),
    Kid("adam", skin=(150, 100, 70, 255), hair=(30, 22, 20, 255), hair_style="curly",
        shirt=(200, 35, 45, 255), trim=WHITE, shorts=BLACK, socks=BLACK, boots=WHITE, number=4,
        stripes=BLACK, eyes=(60, 40, 30, 255)),
    # Long hair tied back in a tail: brown for Tavus, blond for Alkame.
    Kid("tavus", skin=(240, 200, 160, 255), hair=(115, 70, 40, 255), hair_style="tail",
        shirt=(40, 160, 80, 255), trim=WHITE, shorts=WHITE, socks=(40, 160, 80, 255), boots=BLACK, number=3),
    Kid("alkame", skin=(252, 220, 190, 255), hair=(245, 205, 90, 255), hair_style="tail",
        shirt=(150, 30, 60, 255), trim=WHITE, shorts=WHITE, socks=(150, 30, 60, 255), boots=WHITE, number=2,
        eyes=(70, 120, 180, 255)),
    Kid("hind", skin=(246, 206, 172, 255), hair=(70, 45, 30, 255), hair_style="hijab",
        hijab=(200, 45, 95, 255), shirt=(250, 150, 190, 255), trim=WHITE, shorts=(200, 45, 95, 255),
        socks=(250, 150, 190, 255), boots=(250, 205, 40, 255), number=12, long_sleeves=WHITE, long_shirt=True),
]


def shade(colour, factor):
    return tuple(max(0, min(255, int(c * factor))) for c in colour[:3]) + (255,)


class Pen:
    def __init__(self):
        self.image = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.image)

    @staticmethod
    def _p(points):
        return [(x * S, y * S) for x, y in points]

    def ellipse(self, cx, cy, rx, ry, fill, outline=True):
        if outline:
            self.d.ellipse(self._box(cx, cy, rx + LINE, ry + LINE), fill=INK)
        self.d.ellipse(self._box(cx, cy, rx, ry), fill=fill)

    def _box(self, cx, cy, rx, ry):
        return [(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S]

    def capsule(self, a, b, width, fill, outline=True):
        if outline:
            self._capsule(a, b, width + LINE * 2, INK)
        self._capsule(a, b, width, fill)

    def _capsule(self, a, b, width, fill):
        r = width / 2
        self.d.line(self._p([a, b]), fill=fill, width=int(width * S))
        for x, y in (a, b):
            self.d.ellipse(self._box(x, y, r, r), fill=fill)

    def poly(self, points, fill, outline=True):
        if outline:
            # Outline by drawing the polygon's edges as thick lines first.
            pts = self._p(points + [points[0]])
            self.d.line(pts, fill=INK, width=int(LINE * 2 * S), joint="curve")
        self.d.polygon(self._p(points), fill=fill)

    def arc(self, cx, cy, rx, ry, start, end, fill, width):
        self.d.arc(self._box(cx, cy, rx, ry), start, end, fill=fill, width=int(width * S))

    def done(self):
        return self.image.resize((W, H), Image.LANCZOS)


def polar(origin, length, angle_deg):
    """A point `length` away from origin; 0 degrees points straight down, positive swings forward (right)."""
    a = math.radians(angle_deg)
    return origin[0] + math.sin(a) * length, origin[1] + math.cos(a) * length


@dataclass
class Pose:
    bob: float = 0.0                  # body lift in pixels
    lean: float = 4.0                 # forward lean of the torso, degrees
    near_leg: tuple = (0, 0)          # (hip angle, knee bend)
    far_leg: tuple = (0, 0)
    near_arm: tuple = (0, 0)          # (shoulder angle, elbow bend)
    far_arm: tuple = (0, 0)
    face: str = "smile"               # smile, open, ouch
    crouch: float = 0.0               # lowers the hips for the hit frame


def run_pose(i):
    phi = i / 5 * 2 * math.pi
    swing = 46 * math.sin(phi)
    knee_near = 15 + 75 * max(0.0, -math.sin(phi + 0.9))
    knee_far = 15 + 75 * max(0.0, math.sin(phi + 0.9))
    return Pose(bob=6 * abs(math.cos(phi)), lean=8, near_leg=(swing, knee_near), far_leg=(-swing, knee_far),
                near_arm=(-40 * math.sin(phi), 75), far_arm=(40 * math.sin(phi), 75), face="open")


POSES = {
    "Idle": Pose(lean=0, near_leg=(5, 4), far_leg=(-7, 4), near_arm=(12, 18), far_arm=(-10, 18)),
    "Run_01": run_pose(0), "Run_02": run_pose(1), "Run_03": run_pose(2), "Run_04": run_pose(3), "Run_05": run_pose(4),
    "Jump_Up": Pose(bob=14, lean=4, near_leg=(60, 100), far_leg=(25, 70), near_arm=(72, -12), far_arm=(-72, 12),
                    face="open"),
    "Fall_Air": Pose(bob=10, lean=0, near_leg=(18, 25), far_leg=(-12, 20), near_arm=(55, -25), far_arm=(-60, 25),
                     face="open"),
    "Hit": Pose(lean=-6, crouch=38, near_leg=(70, 125), far_leg=(50, 120), near_arm=(50, 100), far_arm=(30, 100),
                face="ouch"),
}


def draw_leg(pen, kid, hip, angle, bend, back):
    k = 0.82 if back else 1.0
    knee = polar(hip, 44, angle)
    shin_angle = angle - bend
    foot = polar(knee, 42, shin_angle)
    # Sock from the knee down, then the shorts over the thigh and the top of the knee.
    pen.capsule(knee, foot, 27, shade(kid.socks, k))
    band = polar(knee, 9, shin_angle)
    pen.capsule(band, polar(band, 5, shin_angle), 27, shade(kid.trim, k), outline=False)
    pen.capsule(hip, polar(knee, 4, angle), 36, shade(kid.shorts, k))
    # The boot points forward, roughly at a right angle to the shin.
    heel = polar(foot, 4, shin_angle)
    toe = polar(heel, 24, shin_angle + 90)
    pen.capsule((heel[0] - 4, heel[1]), toe, 23, shade(kid.boots, k))
    sole = WHITE if kid.boots != WHITE else BLACK
    pen.capsule((heel[0] - 6, heel[1] + 9), (toe[0] + 2, toe[1] + 9), 5, shade(sole, k), outline=False)


def draw_arm(pen, kid, shoulder, angle, bend, back):
    k = 0.82 if back else 1.0
    elbow = polar(shoulder, 32, angle)
    hand = polar(elbow, 30, angle + bend)
    forearm = kid.long_sleeves or kid.skin
    pen.capsule(elbow, hand, 19, shade(forearm, k))
    sleeve_end = polar(shoulder, 30, angle)
    pen.capsule(shoulder, sleeve_end, 26, shade(kid.shirt, k))
    if kid.gloves:
        pen.ellipse(hand[0], hand[1], 17, 17, shade(kid.gloves, k))
    else:
        pen.ellipse(hand[0], hand[1], 12, 12, shade(kid.skin, k * 0.95))


def draw_head(pen, kid, cx, cy, face):
    r = 72
    style = kid.hair_style

    # Hair or scarf behind the head.
    if style == "hijab":
        # Scarf round the head and a short drape over the shoulders, one outline for both.
        shapes = ((cx - 2, cy + 2, r + 12, r + 12), (cx - 6, cy + 74, 64, 40))
        for x, y, rx, ry in shapes:
            pen.ellipse(x, y, rx + LINE, ry + LINE, INK, outline=False)
        for x, y, rx, ry in shapes:
            pen.ellipse(x, y, rx, ry, kid.hijab, outline=False)
        pen.arc(cx - 6, cy + 74, 52, 30, 120, 170, shade(kid.hijab, 0.85), 4)
    elif style == "pony":
        pen.ellipse(cx - r - 8, cy - 30, 34, 26, kid.hair)
    elif style == "tail":
        # One smooth tail from the back of the head down over the jersey, tied with a
        # band in the kit colour and a couple of strands so it reads as hair.
        tail = [(cx - 50, cy - 20), (cx - 84, cy + 4), (cx - 98, cy + 40), (cx - 98, cy + 78), (cx - 88, cy + 104),
                (cx - 80, cy + 78), (cx - 76, cy + 46), (cx - 64, cy + 20), (cx - 46, cy + 6)]
        pen.poly(tail, kid.hair)
        strand = shade(kid.hair, 0.8)
        pen.d.line(pen._p([(cx - 86, cy + 30), (cx - 90, cy + 62), (cx - 88, cy + 88)]), fill=strand,
                   width=int(2.5 * S), joint="curve")
        pen.capsule((cx - 84, cy + 12), (cx - 66, cy + 4), 10, kid.shirt)
    elif style == "braid":
        for k in range(4):
            pen.ellipse(cx - 52 - k * 4, cy + 40 + k * 26, 16, 16, kid.hair)
        pen.ellipse(cx - 60, cy + 150, 9, 9, kid.trim)
    elif style == "bun":
        pen.ellipse(cx - 40, cy - r + 6, 30, 28, kid.hair)

    if style != "hijab":
        pen.ellipse(cx, cy, r, r, kid.hair)

    # Face: a skin disc shifted forward, so the back of the head stays hair.
    if style == "hijab":
        pen.ellipse(cx + 18, cy + 12, 52, 58, kid.skin)
        pen.arc(cx + 18, cy + 12, 57, 63, 190, 350, kid.hijab, 10)
        pen.arc(cx + 18, cy + 12, 62, 68, 196, 344, shade(kid.hijab, 0.8), 2.5)
    else:
        pen.ellipse(cx + 12, cy + 10, 60, 60, kid.skin)
        # Ear
        pen.ellipse(cx - 6, cy + 16, 8, 11, shade(kid.skin, 0.95))
        draw_hair_front(pen, kid, cx, cy, r)

    # Eye, brow, nose, mouth, blush.
    ex, ey = cx + 40, cy + 8
    if face == "ouch":
        pen.d.line(pen._p([(ex - 12, ey - 8), (ex + 6, ey), (ex - 12, ey + 8)]), fill=INK, width=int(4 * S))
    else:
        pen.ellipse(ex, ey, 14, 19, (255, 255, 255, 255))
        pen.ellipse(ex + 3, ey + 2, 10, 13, kid.eyes, outline=False)
        pen.ellipse(ex + 4, ey + 3, 5.5, 7, (20, 15, 20, 255), outline=False)
        pen.ellipse(ex + 7, ey - 3, 3.5, 3.5, (255, 255, 255, 255), outline=False)
    pen.arc(ex + 2, ey - 26, 14, 8, 200, 330, shade(kid.hair if style != "hijab" else kid.hair, 0.9), 4)
    pen.capsule((cx + 70, cy + 22), (cx + 73, cy + 28), 5, shade(kid.skin, 0.85), outline=False)
    pen.ellipse(cx + 34, cy + 36, 11, 7, (255, 150, 150, 110), outline=False)

    mx, my = cx + 52, cy + 44
    if face == "open":
        pen.d.chord(pen._box(mx, my - 4, 12, 11), 0, 180, fill=(150, 50, 60, 255), outline=INK, width=int(2.5 * S))
    elif face == "ouch":
        pen.ellipse(mx, my, 6, 5, (150, 50, 60, 255))
    else:
        pen.arc(mx, my - 6, 11, 8, 20, 160, INK, 3)

    if kid.glasses:
        pen.d.ellipse(pen._box(ex + 2, ey + 1, 21, 21), outline=(50, 50, 60, 255), width=int(4 * S))
        pen.d.line(pen._p([(ex - 19, ey - 2), (cx - 8, ey - 6)]), fill=(50, 50, 60, 255), width=int(4 * S))



def draw_hair_front(pen, kid, cx, cy, r):
    """Hair over the forehead: a cap over the top of the head with a fringe."""
    hair = kid.hair
    if kid.hair_style == "curly":
        for ax, ay, rr in ((-40, -40, 30), (-5, -58, 30), (30, -50, 28), (55, -28, 22), (-62, -6, 24)):
            pen.ellipse(cx + ax, cy + ay, rr, rr, hair)
        return

    pen.d.chord(pen._box(cx + 2, cy - 6, r + 2, r - 4), 170, 360, fill=hair, outline=INK, width=int(LINE * S))
    if kid.hair_style == "short":
        # A few tufts on the crown, so short hair does not read as a helmet.
        for ax, ay, ang in ((-34, -54, -55), (-10, -64, -30), (14, -64, -5)):
            tip = polar((cx + ax, cy + ay), -15, ang)
            pen.poly([(cx + ax - 12, cy + ay + 6), tip, (cx + ax + 12, cy + ay + 6)], hair)
    # Fringe locks
    for k in range(3):
        x = cx + 22 + k * 18
        pen.poly([(x - 14, cy - 34), (x + 10, cy - 34), (x + 4, cy - 12 + k * 3)], hair, outline=False)


def draw_kid(kid, pose):
    pen = Pen()
    ground = H - 20  # Rukajja's feet sit 12 px (24 at this size) above the frame bottom
    hip_y = ground - 104 + pose.crouch - pose.bob
    hip = (W / 2 - 8, hip_y)
    torso_top = polar(hip, -70, -pose.lean)  # negative length: up
    shoulder = (torso_top[0] - 4, torso_top[1] + 14)

    raised = [(arm, back) for arm, back in ((pose.far_arm, True), (pose.near_arm, False)) if abs(arm[0]) > 95]
    if abs(pose.far_arm[0]) <= 95:
        draw_arm(pen, kid, shoulder, *pose.far_arm, back=True)
    draw_leg(pen, kid, hip, *pose.far_leg, back=True)
    draw_leg(pen, kid, hip, *pose.near_leg, back=False)

    # Jersey: a rounded trapezoid, reaching over the shorts for the longer cut.
    bottom = 18 if not kid.long_shirt else 40
    tx, ty = torso_top
    outline = [(tx - 30, ty + 4), (tx + 30, ty + 4), (hip[0] + 38, hip[1] + bottom), (hip[0] - 38, hip[1] + bottom)]
    pen.poly(outline, kid.shirt)
    pen.ellipse(tx, ty + 10, 31, 16, kid.shirt)
    if kid.stripes:
        mask = Image.new("L", pen.image.size, 0)
        ImageDraw.Draw(mask).polygon(pen._p(outline), fill=255)
        layer = Image.new("RGBA", pen.image.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        for i in range(-3, 4):
            x = tx + i * 18
            ld.polygon(pen._p([(x - 4, ty - 10), (x + 4, ty - 10), (x + 4 + (hip[0] - tx), hip[1] + 60),
                               (x - 4 + (hip[0] - tx), hip[1] + 60)]), fill=kid.stripes)
        pen.image.paste(layer, (0, 0), ImageChops.darker(mask, layer.getchannel("A")))
    pen.capsule((outline[3][0] + 4, outline[3][1] - 4), (outline[2][0] - 4, outline[2][1] - 4), 6, kid.trim,
                outline=False)
    # V collar and the shirt number on the chest.
    pen.poly([(tx + 2, ty + 2), (tx + 26, ty + 2), (tx + 14, ty + 18)], kid.trim, outline=False)
    font = ImageFont.truetype(FONT, 30 * S)
    # On the back, where it stays readable while the near arm swings over the chest.
    cx, cy = (tx + hip[0]) / 2 - 14, (ty + hip[1]) / 2 + 2
    pen.d.text((cx * S, cy * S), str(kid.number), font=font, fill=kid.trim, anchor="mm",
               stroke_width=int(1.5 * S), stroke_fill=INK if kid.trim != BLACK else WHITE)

    if abs(pose.near_arm[0]) <= 95:
        draw_arm(pen, kid, shoulder, *pose.near_arm, back=False)
    draw_head(pen, kid, tx + 8, ty - 60, pose.face)
    for arm, back in raised:
        draw_arm(pen, kid, (shoulder[0], shoulder[1] - 4), *arm, back=back)
    return pen.done()


def portrait(frame):
    """Head and shoulders, square, for the chooser cards."""
    box = (W / 2 - 115, 20, W / 2 + 115, 250)
    return frame.crop(tuple(int(v) for v in box)).resize((256, 256), Image.LANCZOS)


def main(preview=False):
    if preview:
        sheet = Image.new("RGBA", (W * 6, H * 3), (190, 215, 240, 255))
        for row, kid in enumerate(KIDS[int(sys.argv[3]) if len(sys.argv) > 3 else 0:][:3]):
            for col, name in enumerate(("Idle", "Run_01", "Run_03", "Jump_Up", "Fall_Air", "Hit")):
                sheet.alpha_composite(draw_kid(kid, POSES[name]), (col * W, row * H))
        target = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / "Logs" / "characters_preview.png"
        sheet.save(target)
        print(target)
        return

    for kid in KIDS:
        folder = OUT / kid.id
        folder.mkdir(parents=True, exist_ok=True)
        for name, pose in POSES.items():
            draw_kid(kid, pose).save(folder / f"{name}.png")
        portrait(draw_kid(kid, POSES["Idle"])).save(folder / "portrait.png")

    # Rukajja keeps her hand-drawn frames; the chooser only needs her standing pose and face.
    folder = OUT / "rukajja"
    folder.mkdir(parents=True, exist_ok=True)
    idle = Image.open(ROOT / "Assets" / "Art" / "Rukajja" / "Frames" / "Rukajja_Idle.png").convert("RGBA")
    idle.save(folder / "Idle.png")
    idle.crop((8, 0, 133, 125)).resize((256, 256), Image.LANCZOS).save(folder / "portrait.png")
    print(f"{len(KIDS)} kids written to {OUT}")


if __name__ == "__main__":
    main(preview=len(sys.argv) > 1 and sys.argv[1] == "preview")
