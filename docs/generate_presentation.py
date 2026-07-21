"""Genere les videos de presentation bilingues a partir de captures anonymisees."""

from __future__ import annotations

import hashlib
import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parent
MEDIA = ROOT / "media"
CAPTURES = MEDIA / "v3.3"
VERSION = "3.3"

WIDTH = 1920
HEIGHT = 1080
FPS = 30
TRANSITION_SECONDS = 0.45

FONT_REGULAR = Path(r"C:\Windows\Fonts\segoeui.ttf")
FONT_BOLD = Path(r"C:\Windows\Fonts\segoeuib.ttf")


@dataclass(frozen=True)
class Scene:
    key: str
    title: str
    caption: str
    duration: float
    image: str | None = None
    kind: str = "capture"


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_BOLD if bold else FONT_REGULAR), size)


def wrap_text(
    draw: ImageDraw.ImageDraw,
    text: str,
    face: ImageFont.FreeTypeFont,
    max_width: int,
) -> list[str]:
    lines: list[str] = []
    current = ""
    for word in text.split():
        candidate = f"{current} {word}".strip()
        if not current or draw.textlength(candidate, font=face) <= max_width:
            current = candidate
        else:
            lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def fit_image(image: Image.Image, box: tuple[int, int, int, int]) -> tuple[Image.Image, int, int]:
    x, y, width, height = box
    fitted = image.copy()
    fitted.thumbnail((width, height), Image.Resampling.LANCZOS)
    return fitted, x + (width - fitted.width) // 2, y + (height - fitted.height) // 2


def paste_with_shadow(canvas: Image.Image, image: Image.Image, x: int, y: int) -> None:
    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.rounded_rectangle(
        (x + 12, y + 14, x + image.width + 12, y + image.height + 14),
        radius=7,
        fill=(0, 0, 0, 150),
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(14))
    canvas.paste(shadow, (0, 0), shadow)
    canvas.paste(image, (x, y))
    ImageDraw.Draw(canvas).rounded_rectangle(
        (x, y, x + image.width, y + image.height),
        radius=6,
        outline="#516482",
        width=2,
    )


def load_capture(language: str, image_name: str) -> Image.Image:
    capture_path = CAPTURES / language / image_name
    if not capture_path.exists():
        raise FileNotFoundError(f"Capture manquante : {capture_path}")

    image = Image.open(capture_path).convert("RGB")
    if image.size != (1920, 1009):
        raise ValueError(f"Capture inattendue {capture_path}: {image.size}")

    # La barre supérieure contient le chemin local du preset de démonstration.
    # Elle est retirée de toutes les images publiées.
    crop_top = 305 if image_name == "health.png" else 140
    return image.crop((0, crop_top, image.width, image.height))


def draw_brand(draw: ImageDraw.ImageDraw) -> None:
    draw.text((64, 34), f"DCE v{VERSION}", fill="#5CB3FF", font=font(24, True))
    draw.text((1792, 36), "By Mamat", fill="#E7EDF6", font=font(19, True), anchor="ra")
    draw.text((1792, 66), "et ses agents", fill="#91A5C1", font=font(14), anchor="ra")


def draw_caption(draw: ImageDraw.ImageDraw, scene: Scene) -> None:
    draw.rounded_rectangle(
        (48, 870, 1872, 1044),
        radius=7,
        fill="#151E2D",
        outline="#3A4B67",
        width=2,
    )
    draw.rectangle((48, 870, 58, 1044), fill="#2F8AF0")
    draw.text((82, 892), scene.title, fill="#72BDFF", font=font(25, True))

    caption_face = font(35)
    lines = wrap_text(draw, scene.caption, caption_face, 1710)
    if len(lines) > 2:
        raise ValueError(f"Sous-titre trop long pour {scene.key}: {lines}")
    start_y = 936 if len(lines) == 1 else 918
    for index, line in enumerate(lines):
        draw.text((82, start_y + index * 44), line, fill="#F4F7FB", font=caption_face)


def make_intro(language: str) -> Image.Image:
    canvas = Image.new("RGB", (WIDTH, HEIGHT), "#0C121D")
    draw = ImageDraw.Draw(canvas)
    draw_brand(draw)

    if language == "fr":
        heading = "Voir et préparer un preset Dante, hors ligne"
        subtitle = "Une vue rapide, des renommages cohérents et des outils de patch réunis dans un éditeur XML prudent."
        labels = ("Vue d'ensemble", "Patch et Easy patch", "Imports, exports et synoptique")
        warning = "Outil tiers non officiel : travaillez sur une copie et validez toujours l'import final dans Dante Controller."
    else:
        heading = "Review and prepare a Dante preset offline"
        subtitle = "A fast overview, consistent renaming, and patching tools gathered in a cautious XML editor."
        labels = ("Complete overview", "Patch and Easy patch", "Imports, exports and synoptic")
        warning = "Unofficial third-party tool: work on a copy and always validate the final import in Dante Controller."

    draw.text((70, 105), heading, fill="#F7FAFF", font=font(47, True))
    subtitle_lines = wrap_text(draw, subtitle, font(27), 1760)
    for index, line in enumerate(subtitle_lines):
        draw.text((72, 174 + index * 38), line, fill="#BFCBDD", font=font(27))

    previews = (
        ("configuration.png", labels[0]),
        ("easy-patch.png", labels[1]),
        ("synoptic.png", labels[2]),
    )
    for index, (image_name, label) in enumerate(previews):
        left = 56 + index * 615
        source = load_capture(language, image_name)
        fitted, x, y = fit_image(source, (left, 285, 580, 315))
        paste_with_shadow(canvas, fitted, x, y)
        draw.rounded_rectangle((left, 630, left + 580, 770), 7, fill="#151E2D", outline="#3A4B67", width=2)
        draw.rectangle((left, 630, left + 9, 770), fill="#2F8AF0")
        draw.text((left + 30, 675), label, fill="#F5F8FC", font=font(26, True))

    draw.rounded_rectangle((62, 868, 1858, 1007), 7, fill="#241E17", outline="#866431", width=2)
    warning_lines = wrap_text(draw, warning, font(26, True), 1700)
    for index, line in enumerate(warning_lines):
        draw.text((95, 905 + index * 38), line, fill="#FFD48A", font=font(26, True))
    return canvas


def make_end_card(language: str) -> Image.Image:
    canvas = Image.new("RGB", (WIDTH, HEIGHT), "#0C121D")
    draw = ImageDraw.Draw(canvas)
    draw_brand(draw)

    if language == "fr":
        heading = "Dante Config Editor v3.3"
        points = (
            "Travaillez sur une copie du XML.",
            "Contrôlez les différences avant sauvegarde.",
            "Validez l'import avec les outils Dante officiels.",
        )
        label = "Projet gratuit et public"
    else:
        heading = "Dante Config Editor v3.3"
        points = (
            "Work on a copy of the XML.",
            "Review differences before saving.",
            "Validate the import with official Dante tools.",
        )
        label = "Free and public project"

    draw.text((WIDTH // 2, 180), heading, fill="#F7FAFF", font=font(58, True), anchor="ma")
    draw.text((WIDTH // 2, 264), label, fill="#71BDFF", font=font(30, True), anchor="ma")
    draw.rounded_rectangle((330, 340, 1590, 720), 8, fill="#151E2D", outline="#3A4B67", width=2)
    for index, point in enumerate(points, start=1):
        y = 405 + (index - 1) * 92
        draw.ellipse((390, y - 4, 446, y + 52), fill="#2F8AF0")
        draw.text((418, y + 23), str(index), fill="white", font=font(25, True), anchor="mm")
        draw.text((480, y + 23), point, fill="#E9EFF7", font=font(30), anchor="lm")

    draw.text(
        (WIDTH // 2, 820),
        "github.com/Mamat79/DanteConfigEditorV3",
        fill="#5CB3FF",
        font=font(30, True),
        anchor="ma",
    )
    draw.text((WIDTH // 2, 910), "By Mamat", fill="#F7FAFF", font=font(29, True), anchor="ma")
    draw.text((WIDTH // 2, 954), "et ses agents", fill="#9AAEC8", font=font(20), anchor="ma")
    return canvas


def make_capture_slide(language: str, scene: Scene) -> Image.Image:
    if scene.image is None:
        raise ValueError(f"Image manquante pour la scène {scene.key}")

    canvas = Image.new("RGB", (WIDTH, HEIGHT), "#0C121D")
    draw = ImageDraw.Draw(canvas)
    draw_brand(draw)
    draw.text((64, 72), scene.title, fill="#F7FAFF", font=font(43, True))

    source = load_capture(language, scene.image)
    fitted, x, y = fit_image(source, (54, 137, 1812, 700))
    paste_with_shadow(canvas, fitted, x, y)
    draw_caption(draw, scene)
    return canvas


def make_slide(language: str, scene: Scene) -> Image.Image:
    if scene.kind == "intro":
        return make_intro(language)
    if scene.kind == "end":
        return make_end_card(language)
    return make_capture_slide(language, scene)


def scenes(language: str) -> list[Scene]:
    if language == "fr":
        return [
            Scene("intro", "Dante Config Editor v3.3", "", 6.5, kind="intro"),
            Scene("configuration", "Une vue d'ensemble", "Latences, fréquences, modes réseau, IP, Preferred Master et canaux restent visibles sur un même écran.", 7.0, "configuration.png"),
            Scene("patch", "Patch", "Filtrez les récepteurs et les émetteurs, retrouvez une source, puis appliquez ou retirez une subscription.", 7.0, "patch.png"),
            Scene("easy-patch", "Easy patch", "Prévisualisez des sélections ou des plages, accumulez plusieurs opérations, puis appliquez tout le lot en une fois.", 8.0, "easy-patch.png"),
            Scene("labels", "Échange de labels", "Importez et exportez des labels JSON, CSV, DMT XLSX ou ODS, Allen & Heath dLive/Avantis et Yamaha CL/QL.", 8.0, "labels.png"),
            Scene("synoptic", "Synoptique visuel", "Regroupez les machines par emplacement et représentez les patchs consécutifs par des liaisons lisibles, exportables en SVG ou PDF.", 8.0, "synoptic.png"),
            Scene("health", "Santé du fichier", "Repérez rapidement les modes réseau mélangés, les IP fixes, les formats audio et les patchs à vérifier.", 7.0, "health.png"),
            Scene("atomic", "Atomic Bomb", "Composez un exercice de dépannage en choisissant précisément ce qui sera désorganisé dans la copie de travail.", 8.0, "atomic-bomb.png"),
            Scene("end", "Dante Config Editor v3.3", "", 6.5, kind="end"),
        ]

    return [
        Scene("intro", "Dante Config Editor v3.3", "", 6.5, kind="intro"),
        Scene("configuration", "One complete overview", "Latency, sample rates, network modes, IP, Preferred Master, and channels remain visible on one screen.", 7.0, "configuration.png"),
        Scene("patch", "Patch", "Filter receivers and transmitters, find a source, then apply or remove a subscription.", 7.0, "patch.png"),
        Scene("easy-patch", "Easy patch", "Preview selections or exact ranges, accumulate several operations, then apply the entire batch once.", 8.0, "easy-patch.png"),
        Scene("labels", "Channel label exchange", "Import and export JSON, CSV, DMT XLSX or ODS, Allen & Heath dLive/Avantis, and Yamaha CL/QL labels.", 8.0, "labels.png"),
        Scene("synoptic", "Visual synoptic", "Group devices by location and display consecutive subscriptions as readable links, exportable to SVG or PDF.", 8.0, "synoptic.png"),
        Scene("health", "File health", "Quickly spot mixed network modes, static IPs, audio formats, and subscriptions that need review.", 7.0, "health.png"),
        Scene("atomic", "Atomic Bomb", "Build a troubleshooting exercise by choosing exactly what will be scrambled in the working copy.", 8.0, "atomic-bomb.png"),
        Scene("end", "Dante Config Editor v3.3", "", 6.5, kind="end"),
    ]


def format_srt_time(seconds: float) -> str:
    milliseconds = int(round(seconds * 1000))
    hours, milliseconds = divmod(milliseconds, 3_600_000)
    minutes, milliseconds = divmod(milliseconds, 60_000)
    secs, milliseconds = divmod(milliseconds, 1000)
    return f"{hours:02}:{minutes:02}:{secs:02},{milliseconds:03}"


def write_srt(language: str, sequence: list[Scene]) -> None:
    starts = [0.0]
    for scene in sequence[:-1]:
        starts.append(starts[-1] + scene.duration)

    blocks: list[str] = []
    for index, (scene, start) in enumerate(zip(sequence, starts, strict=True), start=1):
        if scene.kind in {"intro", "end"}:
            text = scene.title
        else:
            text = f"{scene.title}\n{scene.caption}"
        end = starts[index] if index < len(starts) else start + scene.duration
        blocks.append(f"{index}\n{format_srt_time(start)} --> {format_srt_time(end)}\n{text}\n")

    output = MEDIA / f"dce-v33-presentation-{language}.srt"
    output.write_text("\n".join(blocks), encoding="utf-8-sig")


def write_checksum(path: Path) -> None:
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    checksum_path = path.with_suffix(path.suffix + ".sha256")
    # Un LF explicite permet a sha256sum de relire le fichier sous Linux.
    checksum_path.write_bytes(f"{digest}  {path.name}\n".encode("ascii"))


def build_video(language: str) -> None:
    try:
        from imageio_ffmpeg import get_ffmpeg_exe
    except ImportError as exc:
        raise SystemExit("Installez imageio-ffmpeg avant de générer les vidéos.") from exc

    sequence = scenes(language)
    output = MEDIA / f"dce-v33-presentation-{language}.mp4"
    with tempfile.TemporaryDirectory(prefix=f"dce-v33-{language}-") as temporary:
        folder = Path(temporary)
        slides: list[Path] = []
        for index, scene in enumerate(sequence, start=1):
            slide_path = folder / f"slide-{index:02}.png"
            make_slide(language, scene).save(slide_path, optimize=True)
            slides.append(slide_path)

        # Chaque scène est encodée avec un fondu court. Cette méthode reste
        # compatible avec les différentes versions de FFmpeg livrées par Codex.
        segments: list[Path] = []
        for index, (slide_path, scene) in enumerate(zip(slides, sequence, strict=True), start=1):
            segment_path = folder / f"segment-{index:02}.mp4"
            fade_out_start = scene.duration - TRANSITION_SECONDS
            segment_command = [
                    get_ffmpeg_exe(),
                    "-y",
                    "-framerate",
                    str(FPS),
                    "-loop",
                    "1",
                    "-t",
                    f"{scene.duration:.3f}",
                    "-i",
                    str(slide_path),
                    "-vf",
                    (
                        f"fade=t=in:st=0:d={TRANSITION_SECONDS:.3f},"
                        f"fade=t=out:st={fade_out_start:.3f}:d={TRANSITION_SECONDS:.3f},"
                        "format=yuv420p"
                    ),
                    "-r",
                    str(FPS),
                    "-c:v",
                    "libx264",
                    "-preset",
                    "medium",
                    "-crf",
                    "20",
                    "-pix_fmt",
                    "yuv420p",
                    str(segment_path),
            ]
            subprocess.run(segment_command, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            segments.append(segment_path)

        concat_file = folder / "segments.txt"
        concat_file.write_text(
            "\n".join(f"file '{segment.as_posix()}'" for segment in segments),
            encoding="utf-8",
        )
        concat_command = [
            get_ffmpeg_exe(),
            "-y",
            "-f",
            "concat",
            "-safe",
            "0",
            "-i",
            str(concat_file),
            "-c",
            "copy",
            "-movflags",
            "+faststart",
            str(output),
        ]
        subprocess.run(concat_command, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    write_srt(language, sequence)
    write_checksum(output)


def main() -> None:
    MEDIA.mkdir(parents=True, exist_ok=True)
    for language in ("fr", "en"):
        build_video(language)


if __name__ == "__main__":
    main()
