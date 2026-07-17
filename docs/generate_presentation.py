from __future__ import annotations

import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parent
MEDIA = ROOT / "media"
GUIDE_MEDIA = MEDIA / "guide"
VERSION = "3.08"

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


def make_slide(language: str, scene: Scene) -> Image.Image:
    source = Image.open(GUIDE_MEDIA / language / scene.image).convert("RGB")
    if scene.overview:
        return source.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)

    canvas = Image.new("RGB", (WIDTH, HEIGHT), "#0B1220")
    draw = ImageDraw.Draw(canvas)
    draw.text((74, 42), f"DANTE CONFIG EDITOR V{VERSION}", fill="#58A6FF", font=font(23, True))
    draw.text((74, 80), scene.title, fill="white", font=font(49, True))

    if scene.end_card:
        draw.rounded_rectangle((160, 230, 1760, 800), 12, fill="#152035", outline="#344867", width=2)
        logo = source.copy()
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
            Scene("overview.png", "Dante Config Editor V3.08", "Voir, corriger et préparer un preset Dante hors ligne.", 8, overview=True),
            Scene("configuration.png", "Une vue d'ensemble", "Latences, fréquences, modes réseau, IP, Preferred Master et canaux sont visibles sur un même écran.", 7),
            Scene("device-details-context.png", "Modifier une machine", "Changez son nom, ses formats audio, son mode réseau, son IP et ses canaux TX/RX depuis une seule fenêtre.", 7),
            Scene("patch.png", "Patch", "Filtrez les émetteurs et récepteurs, recherchez une source, appliquez ou retirez une subscription.", 7),
            Scene("easy-patch.png", "Easy patch", "Prévisualisez une sélection ou une plage, accumulez plusieurs opérations, puis appliquez tout le lot en une fois.", 8),
            Scene("file-health.png", "Repérer les incohérences", "Le logiciel signale les modes réseau mélangés, les IP fixes, les fréquences, les bits et les patchs à vérifier.", 7),
            Scene("safety-log.png", "Contrôler avant la sauvegarde", "Relisez le résumé, les différences, les rapports de compatibilité et l'historique avant d'enregistrer sous un nouveau nom.", 8),
            Scene("overview.png", "Un workflow hors ligne, puis une validation officielle", "Dante Config Editor ne pilote pas le réseau : le XML final doit toujours être importé et validé dans Dante Controller.", 8),
            Scene("overview.png", "Dante Config Editor V3.08", "Projet gratuit et public - By Mamat et ses agents.", 7, end_card=True),
        ]
    return [
        Scene("overview.png", "Dante Config Editor V3.08", "Review, correct, and prepare a Dante preset offline.", 8, overview=True),
        Scene("configuration.png", "One complete overview", "Latency, sample rates, network modes, IP, Preferred Master, and channels are visible on one screen.", 7),
        Scene("device-details-context.png", "Edit one device", "Change its name, audio formats, network mode, IP settings, and TX/RX channels from one window.", 7),
        Scene("patch.png", "Patch", "Filter transmitters and receivers, find a source, then apply or remove a subscription.", 7),
        Scene("easy-patch.png", "Easy patch", "Preview a selection or exact range, accumulate several operations, then apply the entire batch once.", 8),
        Scene("file-health.png", "Spot inconsistencies", "The application reports mixed network modes, static IPs, sample rates, bit depths, and subscriptions to review.", 7),
        Scene("safety-log.png", "Review before saving", "Inspect the summary, differences, compatibility reports, and history before saving under a new name.", 8),
        Scene("overview.png", "Offline workflow, official validation", "Dante Config Editor does not control the network: always import and validate the final XML in Dante Controller.", 8),
        Scene("overview.png", "Dante Config Editor V3.08", "Free and public project - By Mamat et ses agents.", 7, end_card=True),
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
    (MEDIA / f"dante-config-editor-v308-overview-{language}.srt").write_text("\n".join(blocks), encoding="utf-8-sig")


def build_video(language: str) -> None:
    try:
        from imageio_ffmpeg import get_ffmpeg_exe
    except ImportError as exc:
        raise SystemExit("Install imageio-ffmpeg before generating the presentation video.") from exc

    sequence = scenes(language)
    output = MEDIA / f"dante-config-editor-v308-overview-{language}.mp4"
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
