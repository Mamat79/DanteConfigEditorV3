from __future__ import annotations

import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parent
MEDIA = ROOT / "media"
GUIDE_MEDIA = MEDIA / "guide"
VERSION = "3.09"

FONT_REGULAR = Path(r"C:\Windows\Fonts\segoeui.ttf")
FONT_BOLD = Path(r"C:\Windows\Fonts\segoeuib.ttf")

WIDTH = 1920
HEIGHT = 1080
FPS = 30


@dataclass(frozen=True)
class Scene:
    image: str
    title: str
    caption: str
    duration: float
    overview: bool = False
    end_card: bool = False
    atomic: bool = False


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_BOLD if bold else FONT_REGULAR), size)


def wrap(draw: ImageDraw.ImageDraw, text: str, face: ImageFont.FreeTypeFont, max_width: int) -> list[str]:
    words = text.split()
    lines: list[str] = []
    current = ""
    for word in words:
        candidate = f"{current} {word}".strip()
        if not current or draw.textlength(candidate, font=face) <= max_width:
            current = candidate
        else:
            lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def fit(image: Image.Image, box: tuple[int, int, int, int]) -> tuple[Image.Image, int, int]:
    x, y, width, height = box
    fitted = image.copy()
    fitted.thumbnail((width, height), Image.Resampling.LANCZOS)
    return fitted, x + (width - fitted.width) // 2, y + (height - fitted.height) // 2


def paste_with_shadow(canvas: Image.Image, image: Image.Image, x: int, y: int) -> None:
    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(shadow)
    draw.rounded_rectangle((x + 12, y + 14, x + image.width + 12, y + image.height + 14), 8, fill=(0, 0, 0, 150))
    shadow = shadow.filter(ImageFilter.GaussianBlur(14))
    canvas.paste(shadow, (0, 0), shadow)
    canvas.paste(image, (x, y))
    ImageDraw.Draw(canvas).rounded_rectangle((x, y, x + image.width, y + image.height), 7, outline="#526787", width=2)


def main_content(image: Image.Image, image_name: str) -> Image.Image:
    if image_name in {"configuration.png", "patch.png", "easy-patch.png", "file-health.png", "safety-log.png"}:
        return image.crop((0, min(145, image.height - 1), image.width, image.height))
    return image


def make_overview(language: str) -> Image.Image:
    canvas = Image.new("RGB", (WIDTH, HEIGHT), "#0B1220")
    draw = ImageDraw.Draw(canvas)
    title = (
        "Contrôler et préparer un preset Dante, hors ligne"
        if language == "fr"
        else "Review and prepare a Dante preset offline"
    )
    subtitle = (
        "Une vue d'ensemble rapide, des renommages cohérents et des outils de patch sécurisés."
        if language == "fr"
        else "A fast overview, consistent renaming, and safer patching tools."
    )
    draw.text((70, 60), f"DANTE CONFIG EDITOR V{VERSION}", fill="#58A6FF", font=font(24, True))
    draw.text((70, 112), title, fill="white", font=font(47, True))
    draw.text((70, 180), subtitle, fill="#BCC9DB", font=font(26))

    panels = [
        ("configuration.png", "Configuration", "Tout voir rapidement" if language == "fr" else "Review everything quickly"),
        ("patch.png", "Patch", "Renommer sans perdre le patch" if language == "fr" else "Rename without losing the patch"),
        ("file-health.png", "Santé du fichier" if language == "fr" else "File health", "Préparer hors ligne" if language == "fr" else "Prepare offline"),
    ]
    for index, (image_name, label, feature) in enumerate(panels):
        left = 55 + index * 615
        source = main_content(Image.open(GUIDE_MEDIA / language / image_name).convert("RGB"), image_name)
        fitted, x, y = fit(source, (left, 250, 580, 330))
        paste_with_shadow(canvas, fitted, x, y)
        draw.text((left + 22, 600), label, fill="white", font=font(27, True))
        draw.rounded_rectangle((left, 650, left + 580, 815), 7, fill="#152035", outline="#344867", width=2)
        draw.rectangle((left, 650, left + 10, 815), fill="#2584E8")
        draw.text((left + 35, 690), feature, fill="white", font=font(27, True))

    warning = (
        "Outil tiers non officiel - travaillez sur une copie et validez l'import final."
        if language == "fr"
        else "Unofficial third-party tool - work on a copy and validate the final import."
    )
    draw.text((70, 930), warning, fill="#FFBE62", font=font(22, True))
    draw.text((70, 980), "github.com/Mamat79/DanteConfigEditorV3", fill="#91A6C2", font=font(20))
    draw.text((1610, 960), "By Mamat", fill="white", font=font(22, True))
    draw.text((1620, 992), "et ses agents", fill="#91A6C2", font=font(16))
    return canvas


def make_atomic_visual(language: str) -> Image.Image:
    canvas = Image.new("RGB", (WIDTH, HEIGHT), "#0B1220")
    draw = ImageDraw.Draw(canvas)
    draw.text((74, 42), f"DANTE CONFIG EDITOR V{VERSION}", fill="#58A6FF", font=font(23, True))
    heading = "Créer un exercice à réparer" if language == "fr" else "Create a repair exercise"
    draw.text((74, 80), heading, fill="white", font=font(49, True))

    draw.rounded_rectangle((210, 175, 1710, 820), 12, fill="#211317", outline="#B82A2A", width=3)
    panel_title = "GÉNÉRATEUR D'EXERCICE HORS LIGNE" if language == "fr" else "OFFLINE TRAINING SCENARIO GENERATOR"
    draw.text((WIDTH // 2, 220), panel_title, fill="#FECACA", font=font(27, True), anchor="ma")
    draw.ellipse((752, 288, 1168, 704), fill="#050505")
    draw.ellipse((770, 270, 1150, 650), fill="#C8CDD3", outline="#51555B", width=8)
    draw.ellipse((808, 308, 1112, 612), fill="#B00808", outline="#700000", width=7)
    draw.ellipse((826, 322, 1094, 590), fill="#EE2020")
    draw.ellipse((862, 338, 1058, 414), fill="#FF6A5F")
    draw.text((WIDTH // 2, 425), "ATOMIC", fill="#FFF4F4", font=font(28, True), anchor="mm")
    draw.text((WIDTH // 2, 490), "BOMB", fill="white", font=font(54, True), anchor="mm")

    locks = (
        ["Conséquences expliquées", "Identifiants protégés", "Confirmation finale"]
        if language == "fr"
        else ["Consequences explained", "Identifiers protected", "Final confirmation"]
    )
    for index, label in enumerate(locks, start=1):
        left = 300 + (index - 1) * 465
        draw.ellipse((left, 670, left + 62, 732), fill="#8B1A1A", outline="#FF8A80", width=2)
        draw.text((left + 31, 701), str(index), fill="white", font=font(27, True), anchor="mm")
        draw.text((left + 82, 701), label, fill="#F7D6D6", font=font(24, True), anchor="lm")

    safety = (
        "Le fichier original n'est jamais modifié. Enregistrer sous reste obligatoire."
        if language == "fr"
        else "The original file is never modified. Save As remains mandatory."
    )
    draw.text((WIDTH // 2, 780), safety, fill="white", font=font(27), anchor="ma")
    return canvas


def make_slide(language: str, scene: Scene) -> Image.Image:
    if scene.overview:
        return make_overview(language)
    if scene.atomic:
        canvas = make_atomic_visual(language)
        draw = ImageDraw.Draw(canvas)
        draw.rounded_rectangle((48, 888, 1872, 1045), 8, fill="#152035", outline="#344867", width=2)
        caption_font = font(32)
        lines = wrap(draw, scene.caption, caption_font, 1700)
        start_y = 965 - (len(lines) * 43) // 2
        for index, line in enumerate(lines):
            draw.text((WIDTH // 2, start_y + index * 43), line, fill="#EDF4FC", font=caption_font, anchor="ma")
        return canvas

    source = Image.open(GUIDE_MEDIA / language / scene.image).convert("RGB")
    source = main_content(source, scene.image)

    canvas = Image.new("RGB", (WIDTH, HEIGHT), "#0B1220")
    draw = ImageDraw.Draw(canvas)
    draw.text((74, 42), f"DANTE CONFIG EDITOR V{VERSION}", fill="#58A6FF", font=font(23, True))
    draw.text((74, 80), scene.title, fill="white", font=font(49, True))

    if scene.end_card:
        draw.rounded_rectangle((160, 230, 1760, 800), 12, fill="#152035", outline="#344867", width=2)
        logo = make_overview(language)
        logo.thumbnail((760, 430), Image.Resampling.LANCZOS)
        canvas.paste(logo, (220, 300))
        right_x = 1050
        end_title = "Prêt à préparer votre prochain preset" if language == "fr" else "Ready to prepare your next preset"
        draw.text((right_x, 330), end_title, fill="white", font=font(38, True))
        end_lines = (
            ["Travaillez sur une copie.", "Vérifiez les différences.", "Validez l'import dans Dante Controller."]
            if language == "fr"
            else ["Work on a copy.", "Review the differences.", "Validate the import in Dante Controller."]
        )
        for index, line in enumerate(end_lines):
            draw.text((right_x, 420 + index * 58), f"•  {line}", fill="#C9D6E7", font=font(27))
        draw.text((right_x, 650), "github.com/Mamat79/DanteConfigEditorV3", fill="#58A6FF", font=font(25, True))
    else:
        fitted, x, y = fit(source, (80, 150, 1760, 710))
        paste_with_shadow(canvas, fitted, x, y)

    draw.rounded_rectangle((48, 888, 1872, 1045), 8, fill="#152035", outline="#344867", width=2)
    caption_font = font(32)
    lines = wrap(draw, scene.caption, caption_font, 1700)
    total_height = len(lines) * 43
    start_y = 965 - total_height // 2
    for index, line in enumerate(lines):
        draw.text((WIDTH // 2, start_y + index * 43), line, fill="#EDF4FC", font=caption_font, anchor="ma")
    return canvas


def scenes(language: str) -> list[Scene]:
    if language == "fr":
        return [
            Scene("overview.png", "Dante Config Editor V3.09", "Voir, corriger et préparer un preset Dante hors ligne.", 8, overview=True),
            Scene("configuration.png", "Une vue d'ensemble", "Latences, fréquences, modes réseau, IP, Preferred Master et canaux sont visibles sur un même écran.", 7),
            Scene("device-details-context.png", "Modifier une machine", "Changez son nom, ses formats audio, son mode réseau, son IP et ses canaux TX/RX depuis une seule fenêtre.", 7),
            Scene("patch.png", "Patch", "Filtrez les émetteurs et récepteurs, recherchez une source, appliquez ou retirez une subscription.", 7),
            Scene("easy-patch.png", "Easy patch", "Prévisualisez une sélection ou une plage, accumulez plusieurs opérations, puis appliquez tout le lot en une fois.", 8),
            Scene("file-health.png", "Repérer les incohérences", "Le logiciel signale les modes réseau mélangés, les IP fixes, les fréquences, les bits et les patchs à vérifier.", 7),
            Scene("safety-log.png", "Créer un exercice à réparer", "Atomic Bomb mélange volontairement la copie XML après trois confirmations, sans toucher au fichier original.", 8, atomic=True),
            Scene("file-health.png", "Un workflow hors ligne, puis une validation officielle", "Dante Config Editor ne pilote pas le réseau : le XML final doit toujours être importé et validé dans Dante Controller.", 8),
            Scene("overview.png", "Dante Config Editor V3.09", "Projet gratuit et public - By Mamat et ses agents.", 7, end_card=True),
        ]
    return [
        Scene("overview.png", "Dante Config Editor V3.09", "Review, correct, and prepare a Dante preset offline.", 8, overview=True),
        Scene("configuration.png", "One complete overview", "Latency, sample rates, network modes, IP, Preferred Master, and channels are visible on one screen.", 7),
        Scene("device-details-context.png", "Edit one device", "Change its name, audio formats, network mode, IP settings, and TX/RX channels from one window.", 7),
        Scene("patch.png", "Patch", "Filter transmitters and receivers, find a source, then apply or remove a subscription.", 7),
        Scene("easy-patch.png", "Easy patch", "Preview a selection or exact range, accumulate several operations, then apply the entire batch once.", 8),
        Scene("file-health.png", "Spot inconsistencies", "The application reports mixed network modes, static IPs, sample rates, bit depths, and subscriptions to review.", 7),
        Scene("safety-log.png", "Create a repair exercise", "Atomic Bomb deliberately scrambles the XML copy after three confirmations without touching the original file.", 8, atomic=True),
        Scene("file-health.png", "Offline workflow, official validation", "Dante Config Editor does not control the network: always import and validate the final XML in Dante Controller.", 8),
        Scene("overview.png", "Dante Config Editor V3.09", "Free and public project - By Mamat et ses agents.", 7, end_card=True),
    ]


def format_time(seconds: float) -> str:
    milliseconds = int(round(seconds * 1000))
    hours, milliseconds = divmod(milliseconds, 3_600_000)
    minutes, milliseconds = divmod(milliseconds, 60_000)
    secs, milliseconds = divmod(milliseconds, 1000)
    return f"{hours:02}:{minutes:02}:{secs:02},{milliseconds:03}"


def write_srt(language: str, sequence: list[Scene]) -> None:
    current = 0.0
    blocks: list[str] = []
    for index, scene in enumerate(sequence, start=1):
        blocks.append(
            f"{index}\n{format_time(current)} --> {format_time(current + scene.duration)}\n{scene.title}\n{scene.caption}\n"
        )
        current += scene.duration
    (MEDIA / f"dante-config-editor-v309-overview-{language}.srt").write_text("\n".join(blocks), encoding="utf-8-sig")


def build_video(language: str) -> None:
    try:
        from imageio_ffmpeg import get_ffmpeg_exe
    except ImportError as exc:
        raise SystemExit("Install imageio-ffmpeg before generating the presentation video.") from exc

    sequence = scenes(language)
    output = MEDIA / f"dante-config-editor-v309-overview-{language}.mp4"
    with tempfile.TemporaryDirectory(prefix="dante-presentation-") as temporary:
        folder = Path(temporary)
        concat_lines: list[str] = []
        last_slide: Path | None = None
        for index, scene in enumerate(sequence, start=1):
            slide = folder / f"slide-{index:02}.png"
            make_slide(language, scene).save(slide, optimize=True)
            concat_lines.extend([f"file '{slide.as_posix()}'", f"duration {scene.duration:.3f}"])
            last_slide = slide
        if last_slide is not None:
            concat_lines.append(f"file '{last_slide.as_posix()}'")
        concat = folder / "slides.txt"
        concat.write_text("\n".join(concat_lines), encoding="utf-8")
        command = [
            get_ffmpeg_exe(),
            "-y",
            "-f",
            "concat",
            "-safe",
            "0",
            "-i",
            str(concat),
            "-vf",
            f"fps={FPS},format=yuv420p",
            "-c:v",
            "libx264",
            "-preset",
            "medium",
            "-crf",
            "20",
            "-movflags",
            "+faststart",
            "-t",
            f"{sum(scene.duration for scene in sequence):.3f}",
            str(output),
        ]
        subprocess.run(command, check=True)
    write_srt(language, sequence)


if __name__ == "__main__":
    for language_code in ("fr", "en"):
        build_video(language_code)
