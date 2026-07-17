from __future__ import annotations

from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    KeepTogether,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parent
# Les quatre PDF sont générés depuis une source unique pour garder les versions
# française et anglaise synchronisées avec l'application et l'installateur.
PRODUCT = "Dante Config Editor V3.08"
VERSION = "3.08"
GITHUB = "github.com/Mamat79/DanteConfigEditorV3"

INK = colors.HexColor("#172033")
MUTED = colors.HexColor("#526070")
ACCENT = colors.HexColor("#1677D2")
LINE = colors.HexColor("#D7DEE8")
PALE_BLUE = colors.HexColor("#EEF6FF")
PALE_RED = colors.HexColor("#FFF1F1")
PALE_GREEN = colors.HexColor("#EDF8F2")


def register_fonts() -> tuple[str, str]:
    candidates = [
        (Path(r"C:\Windows\Fonts\segoeui.ttf"), Path(r"C:\Windows\Fonts\segoeuib.ttf")),
        (Path(r"C:\Windows\Fonts\arial.ttf"), Path(r"C:\Windows\Fonts\arialbd.ttf")),
    ]
    for regular, bold in candidates:
        if regular.exists() and bold.exists():
            pdfmetrics.registerFont(TTFont("GuideRegular", str(regular)))
            pdfmetrics.registerFont(TTFont("GuideBold", str(bold)))
            return "GuideRegular", "GuideBold"
    return "Helvetica", "Helvetica-Bold"


REGULAR, BOLD = register_fonts()
BASE = getSampleStyleSheet()
STYLES = {
    "title": ParagraphStyle(
        "GuideTitle",
        parent=BASE["Title"],
        fontName=BOLD,
        fontSize=21,
        leading=25,
        textColor=INK,
        alignment=TA_CENTER,
        spaceAfter=3 * mm,
    ),
    "subtitle": ParagraphStyle(
        "GuideSubtitle",
        parent=BASE["Normal"],
        fontName=REGULAR,
        fontSize=10.5,
        leading=14,
        textColor=MUTED,
        alignment=TA_CENTER,
        spaceAfter=4 * mm,
    ),
    "h1": ParagraphStyle(
        "GuideH1",
        parent=BASE["Heading1"],
        fontName=BOLD,
        fontSize=14,
        leading=17,
        textColor=INK,
        spaceBefore=2.5 * mm,
        spaceAfter=2 * mm,
    ),
    "h2": ParagraphStyle(
        "GuideH2",
        parent=BASE["Heading2"],
        fontName=BOLD,
        fontSize=10.5,
        leading=13,
        textColor=ACCENT,
        spaceBefore=2 * mm,
        spaceAfter=1.2 * mm,
    ),
    "body": ParagraphStyle(
        "GuideBody",
        parent=BASE["BodyText"],
        fontName=REGULAR,
        fontSize=9.1,
        leading=12.3,
        textColor=INK,
        spaceAfter=1.6 * mm,
    ),
    "small": ParagraphStyle(
        "GuideSmall",
        parent=BASE["BodyText"],
        fontName=REGULAR,
        fontSize=8.2,
        leading=10.8,
        textColor=MUTED,
    ),
    "bullet": ParagraphStyle(
        "GuideBullet",
        parent=BASE["BodyText"],
        fontName=REGULAR,
        fontSize=8.9,
        leading=12,
        leftIndent=4 * mm,
        firstLineIndent=-3 * mm,
        textColor=INK,
        spaceAfter=1.1 * mm,
    ),
    "table_header": ParagraphStyle(
        "GuideTableHeader",
        parent=BASE["BodyText"],
        fontName=BOLD,
        fontSize=8.3,
        leading=10.5,
        textColor=colors.white,
    ),
    "table": ParagraphStyle(
        "GuideTable",
        parent=BASE["BodyText"],
        fontName=REGULAR,
        fontSize=8.2,
        leading=10.5,
        textColor=INK,
    ),
}


def para(text: str, style: str = "body") -> Paragraph:
    return Paragraph(text, STYLES[style])


def bullets(items: list[str]) -> list[Paragraph]:
    return [para(f"- {item}", "bullet") for item in items]


def callout(text: str, background: colors.Color = PALE_RED) -> Table:
    table = Table([[para(text, "body")]], colWidths=[170 * mm])
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), background),
                ("BOX", (0, 0), (-1, -1), 0.6, LINE),
                ("LEFTPADDING", (0, 0), (-1, -1), 4 * mm),
                ("RIGHTPADDING", (0, 0), (-1, -1), 4 * mm),
                ("TOPPADDING", (0, 0), (-1, -1), 2.5 * mm),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 2.5 * mm),
            ]
        )
    )
    return table


def data_table(headers: list[str], rows: list[list[str]], widths: list[float]) -> Table:
    content = [[para(header, "table_header") for header in headers]]
    content.extend([[para(cell, "table") for cell in row] for row in rows])
    table = Table(content, colWidths=[width * mm for width in widths], repeatRows=1)
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), ACCENT),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, PALE_BLUE]),
                ("GRID", (0, 0), (-1, -1), 0.45, LINE),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 2.4 * mm),
                ("RIGHTPADDING", (0, 0), (-1, -1), 2.4 * mm),
                ("TOPPADDING", (0, 0), (-1, -1), 1.8 * mm),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 1.8 * mm),
            ]
        )
    )
    return table


def draw_header_footer(canvas, doc) -> None:
    canvas.saveState()
    width, height = A4
    canvas.setStrokeColor(LINE)
    canvas.setLineWidth(0.5)
    canvas.line(18 * mm, height - 15 * mm, width - 18 * mm, height - 15 * mm)
    canvas.setFont(REGULAR, 7.4)
    canvas.setFillColor(MUTED)
    canvas.drawString(18 * mm, height - 11.5 * mm, f"{PRODUCT} - version {VERSION} - By Mamat et ses agents")
    canvas.drawRightString(width - 18 * mm, 10 * mm, f"Page {doc.page}")
    canvas.line(18 * mm, 14 * mm, width - 18 * mm, 14 * mm)
    canvas.restoreState()


def build_document(path: Path, story: list) -> None:
    document = SimpleDocTemplate(
        str(path),
        pagesize=A4,
        leftMargin=20 * mm,
        rightMargin=20 * mm,
        topMargin=21 * mm,
        bottomMargin=18 * mm,
        title=PRODUCT,
        author="Mamat et ses agents",
        subject="Offline Dante XML editor user guide",
    )
    document.build(story, onFirstPage=draw_header_footer, onLaterPages=draw_header_footer)


def quick_start(language: str) -> None:
    french = language == "FR"
    subtitle = (
        "Démarrage rapide - édition hors ligne de fichiers XML Dante"
        if french
        else "Quick start - offline editing of Dante XML files"
    )
    warning = (
        "<b>Outil tiers non officiel Audinate.</b> La V3.08 est la version officielle courante pour Windows et macOS, mais elle peut encore contenir des bugs. La V3.07 demeure disponible dans l'historique. Travaillez sur une copie et validez toujours le XML final par un import dans l'outil Dante officiel adapté avant toute utilisation réelle."
        if french
        else "<b>Third-party tool, not an official Audinate product.</b> V3.08 is the current official version for Windows and macOS, but it may still contain bugs. V3.07 remains available in history. Work on a copy and always validate the final XML by importing it into the appropriate official Dante tool before real use."
    )
    steps = (
        [
            ("Ouvrir XML", "Choisissez un export Dante. L'application travaille hors ligne et n'accède pas au réseau."),
            ("Contrôler les alertes", "Affichez les machines concernées et vérifiez chaque point signalé."),
            ("Modifier", "Utilisez Détail machine, Easy patch sous Windows, l'atelier visuel sur Mac ou les actions globales. Verrouillez les machines à exclure."),
            ("Vérifier", "Utilisez Modifiées uniquement puis Avant / après. Les changements techniques inconnus sont bloqués."),
            ("Enregistrer sous", "Choisissez un nouveau nom. La destination est remplacée atomiquement et sauvegardée si elle existait."),
            ("Tester l'import", "Importez le résultat dans Dante Controller sur une copie de travail avant toute intervention terrain."),
        ]
        if french
        else [
            ("Open XML", "Choose a Dante export. The application works offline and does not access the network."),
            ("Review alerts", "Show affected devices and verify every reported item."),
            ("Edit", "Use Device details, Easy patch on Windows, the visual patch workshop on Mac, or global actions. Lock devices that must be excluded."),
            ("Review", "Use Modified only and Before / after. Unknown technical changes are blocked."),
            ("Save as", "Choose a new name. The destination is replaced atomically and backed up when it already exists."),
            ("Test the import", "Import the result into Dante Controller on a working copy before any field operation."),
        ]
    )
    features = (
        [
            ("Patch visuel", "Sous Windows, chaque prévisualisation rejoint un lot cumulatif et la grille compacte accepte les séries par glissement. Sur Mac, utilisez l'atelier visuel Avalonia."),
            ("Récupération", "Une copie est écrite en arrière-plan après un court délai. La nouvelle destination devient la référence après Enregistrer sous."),
            ("Sous-projet", "Ajouter XML au projet importe les machines uniques et ne propose un renommage que pour les doublons."),
            ("IPv4", "Seule l'interface principale est ciblée. DNS et interface secondaire ne sont pas réécrits implicitement."),
        ]
        if french
        else [
            ("Visual patch", "On Windows, every preview joins a cumulative batch and the compact matrix supports drag ranges. On Mac, use the Avalonia visual workshop."),
            ("Recovery", "A copy is written in the background after a short delay. Save as makes the new destination the session reference."),
            ("Sub-project", "Add XML to project imports unique devices and only asks about conflicting names."),
            ("IPv4", "Only the primary interface is targeted. DNS and the secondary interface are not rewritten implicitly."),
        ]
    )

    story = [para(PRODUCT, "title"), para(subtitle, "subtitle"), callout(warning), Spacer(1, 3 * mm)]
    story.append(para("Le parcours recommandé" if french else "Recommended workflow", "h1"))
    step_rows = []
    for number, (heading, text) in enumerate(steps, start=1):
        badge = para(f"<font color='#1677D2'><b>{number}</b></font>", "h1")
        detail = para(f"<b>{heading}</b><br/>{text}", "small")
        step_rows.append([badge, detail])
    step_table = Table(step_rows, colWidths=[12 * mm, 158 * mm])
    step_table.setStyle(
        TableStyle(
            [
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LINEBELOW", (1, 0), (1, -2), 0.35, LINE),
                ("TOPPADDING", (0, 0), (-1, -1), 1.2 * mm),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 1.2 * mm),
            ]
        )
    )
    story.extend([step_table, Spacer(1, 2.5 * mm), para("Fonctions utiles" if french else "Useful features", "h1")])
    feature_cells = []
    for heading, text in features:
        feature_cells.append(para(f"<b>{heading}</b><br/>{text}", "small"))
    feature_table = Table([feature_cells[:2], feature_cells[2:]], colWidths=[84 * mm, 84 * mm])
    feature_table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), PALE_BLUE),
                ("GRID", (0, 0), (-1, -1), 0.45, LINE),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 3 * mm),
                ("RIGHTPADDING", (0, 0), (-1, -1), 3 * mm),
                ("TOPPADDING", (0, 0), (-1, -1), 2 * mm),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 2 * mm),
            ]
        )
    )
    reminder = (
        "<b>À retenir :</b> aucun pilotage en temps réel, aucune API Audinate, et aucune garantie pour tous les formats XML. L'import dans les outils officiels reste indispensable."
        if french
        else "<b>Remember:</b> no real-time control, no Audinate API, and no guarantee for every XML format. Import validation in official tools remains mandatory."
    )
    story.extend([feature_table, Spacer(1, 2.5 * mm), callout(reminder, PALE_GREEN), Spacer(1, 2 * mm), para(("Dépôt public : " if french else "Public repository: ") + GITHUB, "small")])
    build_document(ROOT / f"QuickStart_DanteConfigEditorV3_{language}.pdf", story)


def full_guide(language: str) -> None:
    french = language == "FR"
    if french:
        page1 = [
            para(PRODUCT, "title"),
            para(f"Notice complète - version {VERSION}", "subtitle"),
            callout("<b>Important :</b> cette application est un outil tiers non officiel Audinate. La V3.08 est la version officielle courante pour Windows et macOS, mais elle peut encore contenir des bugs. Elle édite des XML hors ligne, sans connexion au réseau Dante ni API Audinate. Conservez l'original et validez le fichier généré dans Dante Controller avant toute utilisation en production."),
            para("1. Installation et démarrage", "h1"),
            para("L'installateur Windows x64 contient l'application et le runtime .NET 8 nécessaire. Il n'est normalement pas nécessaire d'installer .NET séparément."),
            *bullets([
                "L'installation proposée par défaut se trouve dans Program Files et crée un raccourci dans le menu Démarrer.",
                "La V3.08 utilise son propre dossier Program Files, son propre raccourci et son propre AppId.",
                "Elle peut cohabiter avec la V3.07 et met à niveau une précédente V3.08 Beta ou V3.08.",
                "Deux DMG autonomes V3.08 sont fournis pour macOS : Apple Silicon et Intel. Ils sont signés ad hoc mais non notariés par Apple.",
                "Les quatre notices PDF françaises et anglaises sont installées et restent accessibles depuis l'application.",
            ]),
            para("2. Principes de sécurité", "h1"),
            *bullets([
                "Travaillez sur une copie du XML exporté et utilisez Enregistrer sous.",
                "Le garde-fou suit les machines par identité technique stable, bloque les chemins inconnus et protège les Dante Id, mediaType et instance_id.",
                "La destination est remplacée atomiquement. Le fichier source et toute destination existante reçoivent une copie dans DanteConfigEditor_Backups.",
                "L'import réussi dans Dante Controller constitue la validation finale avant exploitation.",
            ]),
            para("3. Ouvrir un projet", "h1"),
            para("Cliquez sur Ouvrir XML, sélectionnez le fichier, puis contrôlez les compteurs de machines, canaux TX/RX et patchs actifs. Les XML avec namespace par défaut sont pris en charge. La langue et le thème restent modifiables à tout moment."),
        ]
        page2 = [
            para("4. Page Configuration", "h1"),
            para("La page Configuration rassemble la machine sélectionnée, ses canaux, les actions globales et le tableau général."),
            para("Machine sélectionnée", "h2"),
            *bullets([
                "Modifiez ensemble le nom, le mode réseau, la latence et le preferred master avec Appliquer les paramètres.",
                "Double-cliquez une ligne ou utilisez Détail machine pour régler l'IP, la sample rate, les bits, les canaux TX/RX et le patch de ses entrées RX.",
                "Les changements d'une fiche machine sont appliqués en groupe avec une seule reconstruction du modèle.",
                "Les resets peuvent déconnecter les RX, retirer les patchs utilisant les TX, ou effectuer les deux opérations.",
                "La suppression d'une machine retire aussi les points de patch qui la référencent.",
            ]),
            para("Tableau des machines", "h2"),
            *bullets([
                "La sélection multiple définit la cible Sélection non verrouillée. La colonne Lock protège les machines des actions globales.",
                "Le preferred master peut être coché directement. Réduire les réglages agrandit le tableau.",
            ]),
            para("Recherche, filtres et actions globales", "h2"),
            *bullets([
                "La recherche trouve les machines, canaux et références de patch après au moins deux caractères.",
                "Les listes rapides filtrent modes réseau, latences, sample rates, bits, IP fixes et preferred masters.",
                "Modifiées uniquement affiche les machines touchées ; Avant / après détaille chaque différence.",
                "Choisissez toutes les machines non verrouillées, la sélection ou le filtre affiché. Une prévisualisation précède l'application.",
            ]),
        ]
        page3 = [
            para("5. Alertes navigables", "h1"),
            para("Le bandeau Points à vérifier signale les mélanges redondant/daisychain, IP fixes, sample rates multiples et encodages multiples."),
            *bullets([
                "Cliquez sur Voir les machines, choisissez l'alerte puis examinez les devices filtrés.",
                "Après correction, vérifiez que l'alerte disparaît et consultez Santé du fichier.",
            ]),
            para("6. Profils rapides", "h1"),
            data_table(
                ["Profil", "Réglages appliqués"],
                [
                    ["48 kHz / 24 bit / 1 ms", "IP automatique"],
                    ["48 kHz / 24 bit / 2 ms", "IP automatique"],
                    ["96 kHz / 24 bit / 1 ms", "IP automatique"],
                    ["96 kHz / 24 bit / 2 ms", "IP automatique"],
                    ["48 kHz / 24 bit / 1 ms / Redondant", "Mode redondant et IP automatique"],
                    ["48 kHz / 24 bit / 1 ms / Daisychain", "Mode daisychain et IP automatique"],
                ],
                [75, 95],
            ),
            Spacer(1, 2 * mm),
            callout("Vérifiez que chaque matériel accepte la sample rate, les bits, la latence et le mode demandés.", PALE_RED),
            para("7. Récupération automatique", "h1"),
            para("Après une modification, l'application attend brièvement puis écrit la récupération en arrière-plan, sans bloquer l'interface ni remplacer le XML source."),
            *bullets([
                "À la prochaine ouverture du même XML, choisissez de restaurer ou d'abandonner la session.",
                "Après Enregistrer sous, le nouveau fichier devient la référence des modifications et récupérations suivantes.",
                "La copie disparaît après sauvegarde ou retour à l'original ; celles de plus de 30 jours sont nettoyées.",
            ]),
        ]
        page4 = [
            para("8. Canaux et patchs", "h1"),
            *bullets([
                "Les canaux TX/RX peuvent être renommés individuellement ou par plage avec {00}, {000}, {n} et {device}.",
                "Le renommage d'un TX met à jour tous les alias de subscription reconnus dans le projet.",
                "Les Dante Id ne sont pas renumérotés. Le marqueur local subscribed_device=\".\" est conservé.",
                "L'onglet Easy patch affiche les RX à gauche et les TX à droite. Les menus et flèches permettent de changer rapidement de machine.",
                "Sélectionnez autant de TX que de RX pour un appariement un-à-un, ou un seul TX pour alimenter plusieurs RX.",
                "Plusieurs TX vers un RX et les sélections multiples de tailles différentes sont refusés.",
                "Le patch par plage demande un premier TX, un premier RX et une quantité exacte ; une plage incomplète est entièrement bloquée.",
                "Chaque prévisualisation rejoint automatiquement le lot cumulatif sans modifier le XML. Pour les conflits, choisissez annuler, ignorer ou remplacer.",
                "Appliquer exécute directement la sélection ou la plage avec le lot déjà accumulé ; Appliquer tout le lot valide tout en une seule fois.",
                "Dans la grille compacte, les RX sont en lignes et les TX en colonnes. Cliquez pour une affectation ou maintenez et glissez horizontalement, verticalement ou en diagonale pour une série sûre.",
                "Les changements ajoutés au lot restent en attente jusqu'à Appliquer tout le lot. Ils sont alors exécutés en une seule étape d'annulation.",
                "Dans Détail machine, le menu supérieur passe à une autre machine et protège les modifications non appliquées.",
            ]),
            para("9. Ajouter un XML au projet", "h1"),
            *bullets([
                "Les machines dont le nom est unique sont toujours importées.",
                "Seuls les doublons sont proposés au renommage automatique ou manuel.",
                "Les patchs importés suivent les nouveaux noms des machines renommées.",
            ]),
            para("10. IP et formats audio", "h1"),
            *bullets([
                "L'IP automatique ou fixe est réglable machine par machine ou globalement.",
                "Seule l'interface IPv4 principale, network=0 si elle existe, est ciblée. Une interface secondaire n'est pas modifiée.",
                "Le DNS n'est pas réécrit implicitement. La passerelle ne change que lorsqu'une valeur est fournie par l'action.",
                "Sample rate et bits sont modifiables par machine, globalement ou via un profil.",
            ]),
            callout("Un mauvais réglage peut rendre une machine injoignable ou incompatible. Contrôlez les capacités réelles du matériel.", PALE_RED),
            para("11. Santé, comparaison et exports", "h1"),
            *bullets([
                "Santé du fichier regroupe statistiques, erreurs, warnings, patchs libres/locaux et compatibilité.",
                "La comparaison XML affiche les différences dans un tableau.",
                "Les exports TXT/PDF portent la version du logiciel et la signature By Mamat et ses agents.",
            ]),
        ]
        page5 = [
            para("12. Sauvegarde et validation finale", "h1"),
            para("Utilisez Enregistrer sous. Le XML temporaire est relu, le garde-fou vérifie les changements, puis la destination est remplacée atomiquement. Une erreur avant le remplacement laisse l'ancienne destination intacte."),
            data_table(
                ["Contrôle", "Action recommandée"],
                [
                    ["Points à vérifier", "Ouvrir les machines concernées et justifier ou corriger chaque écart."],
                    ["Modifiées uniquement", "Vérifier que seules les machines attendues apparaissent."],
                    ["Avant / après", "Relire les paramètres, canaux et patchs touchés."],
                    ["Dante Controller", "Importer le fichier sur une copie de travail avant toute intervention terrain."],
                ],
                [48, 122],
            ),
            para("13. Tests de non-régression", "h1"),
            para("La suite V3.08 exécute 95 tests Core et contrats Windows, plus 8 tests Avalonia sans écran : identités techniques, chemins inconnus, sauvegarde atomique, récupération, interfaces IPv4, alias de subscription, namespace par défaut, presets synthétiques, lot cumulatif, gestes de matrice, conflits, rollback, persistance, Easy patch, détail machine et identité Mac. GitHub Actions les rejoue sur Windows et macOS."),
            para("14. Limites connues", "h1"),
            *bullets([
                "Aucun pilotage en temps réel et aucune communication avec les appareils.",
                "Aucun SDK/API Audinate et aucun contournement de protocole propriétaire.",
                "La compatibilité dépend de la structure du XML ; seul l'import officiel confirme le fichier final.",
                "L'historique d'annulation conserve au maximum 10 états pour limiter la mémoire.",
                "La matrice affiche uniquement les deux machines choisies pour préserver les performances sur les gros presets.",
                "Les DMG Mac sont signés ad hoc mais non notariés ; le premier lancement peut nécessiter un clic droit puis Ouvrir.",
                "L'onglet Windows Easy patch n'est pas reproduit à l'identique sur Mac, qui conserve l'atelier visuel Avalonia.",
                "Des noms TX dupliqués sont ambigus dans les subscriptions Dante et doivent être renommés avant Easy patch.",
            ]),
            para("15. Aide et informations", "h1"),
            para("Quick start et Notice complète ouvrent automatiquement le PDF français ou anglais selon la langue active."),
            callout(f"Projet public : {GITHUB}<br/>Crédit : By Mamat et ses agents", PALE_GREEN),
        ]
    else:
        page1 = [
            para(PRODUCT, "title"),
            para(f"Full user guide - version {VERSION}", "subtitle"),
            callout("<b>Important:</b> this is a third-party tool, not an official Audinate product. V3.08 is the current official version for Windows and macOS, but it may still contain bugs. It edits XML files offline without connecting to a Dante network or using an Audinate API. Keep the original and validate the generated file in Dante Controller before production use."),
            para("1. Installation and startup", "h1"),
            para("The Windows x64 installer includes the application and the required .NET 8 runtime. A separate .NET installation is normally not required."),
            *bullets([
                "The default location is Program Files, with a Start menu shortcut.",
                "V3.08 uses its own Program Files folder, shortcut, and AppId.",
                "It can coexist with V3.07 and upgrades an earlier V3.08 Beta or V3.08 installation.",
                "Two standalone V3.08 DMGs are provided for macOS: Apple Silicon and Intel. They are ad hoc signed but not notarized by Apple.",
                "All four French and English PDFs are installed and remain available from the application.",
            ]),
            para("2. Safety principles", "h1"),
            *bullets([
                "Work on a copy of the exported XML and use Save as.",
                "The guard tracks devices by stable technical identity, blocks unknown paths, and protects Dante IDs, mediaType, and instance_id.",
                "The destination is replaced atomically. The source and any existing destination receive a copy in DanteConfigEditor_Backups.",
                "A successful import into Dante Controller is the final validation before operation.",
            ]),
            para("3. Open a project", "h1"),
            para("Click Open XML, choose the file, then review device, TX/RX channel, and active subscription counts. XML files with a default namespace are supported. Language and theme can be changed at any time."),
        ]
        page2 = [
            para("4. Configuration page", "h1"),
            para("The Configuration page combines the selected device, its channels, global actions, and the device table."),
            para("Selected device", "h2"),
            *bullets([
                "Apply the name, network mode, latency, and Preferred Master state together with Apply settings.",
                "Double-click a row or use Device details to edit IP settings, sample rate, bits per sample, TX/RX names, and subscriptions for its Rx inputs.",
                "A complete Device details change is grouped into one model rebuild.",
                "Clear actions can disconnect Rx inputs, remove subscriptions using Tx channels, or do both.",
                "Deleting a device also removes subscription points that reference it.",
            ]),
            para("Device table", "h2"),
            *bullets([
                "Multiple selection defines the Selected unlocked target. The Lock column protects devices from global actions.",
                "Preferred Master can be toggled directly. Hide settings enlarges the table.",
            ]),
            para("Search, filters, and global actions", "h2"),
            *bullets([
                "Search finds devices, channels, and subscription references after at least two characters.",
                "Quick lists filter network modes, latencies, sample rates, bits, static IPs, and Preferred Masters.",
                "Modified only shows changed devices; Before / after lists every difference.",
                "Choose all unlocked, selected unlocked, or visible unlocked devices. A preview is shown before application.",
            ]),
        ]
        page3 = [
            para("5. Navigable alerts", "h1"),
            para("The Items to check banner reports mixed redundant/daisychain modes, static IPs, multiple sample rates, and multiple bit depths."),
            *bullets([
                "Click Show devices, choose an alert, then review the filtered devices.",
                "After correcting an item, verify that the alert disappears and review File health.",
            ]),
            para("6. Quick profiles", "h1"),
            data_table(
                ["Profile", "Applied settings"],
                [
                    ["48 kHz / 24 bit / 1 ms", "Automatic IP"],
                    ["48 kHz / 24 bit / 2 ms", "Automatic IP"],
                    ["96 kHz / 24 bit / 1 ms", "Automatic IP"],
                    ["96 kHz / 24 bit / 2 ms", "Automatic IP"],
                    ["48 kHz / 24 bit / 1 ms / Redundant", "Redundant mode and automatic IP"],
                    ["48 kHz / 24 bit / 1 ms / Daisychain", "Daisychain mode and automatic IP"],
                ],
                [75, 95],
            ),
            Spacer(1, 2 * mm),
            callout("Verify that every device supports the requested sample rate, bit depth, latency, and network mode.", PALE_RED),
            para("7. Automatic recovery", "h1"),
            para("After a change, the application waits briefly and writes recovery data in the background without blocking the interface or replacing the source XML."),
            *bullets([
                "When reopening the same XML, choose whether to restore or discard the previous session.",
                "After Save as, the new file becomes the reference for later edits and recovery data.",
                "The copy is deleted after saving or reverting; copies older than 30 days are cleaned automatically.",
            ]),
        ]
        page4 = [
            para("8. Channels and subscriptions", "h1"),
            *bullets([
                "TX/RX channels can be renamed individually or by range with {00}, {000}, {n}, and {device}.",
                "Renaming a Tx channel updates every recognized subscription alias in the project.",
                "Dante IDs are not renumbered. The local subscribed_device=\".\" marker is preserved.",
                "The Easy patch tab shows Rx channels on the left and Tx channels on the right. Menus and arrows switch devices quickly.",
                "Select equal Tx and Rx counts for one-to-one mapping, or one Tx to feed several Rx channels.",
                "Several Tx channels to one Rx and unequal multiple selections are blocked.",
                "Range patching requires a first Tx, first Rx, and exact count; an incomplete range is blocked as a whole.",
                "Every preview automatically joins the cumulative batch without modifying the XML. For conflicts, choose cancel, skip, or replace.",
                "Apply executes the selection or range with the accumulated batch; Apply entire batch validates everything once.",
                "In the compact matrix, Rx channels are rows and Tx channels are columns. Click for one assignment, or hold and drag horizontally, vertically, or diagonally for a safe range.",
                "Changes added to the batch remain pending until Apply entire batch. They are then executed in one undo step.",
                "In Device details, the top menu switches devices and protects unapplied changes.",
            ]),
            para("9. Add XML to project", "h1"),
            *bullets([
                "Devices with unique names are always imported.",
                "Only conflicting names are offered for automatic or manual rename.",
                "Imported subscriptions follow renamed imported devices.",
            ]),
            para("10. IP and audio formats", "h1"),
            *bullets([
                "Automatic or static IP is editable per device or through a global action.",
                "Only the primary IPv4 interface, network=0 when available, is targeted. A secondary interface is not changed.",
                "DNS is not rewritten implicitly. Gateway changes only when the action provides a value.",
                "Sample rate and bits per sample are editable per device, globally, or through a profile.",
            ]),
            callout("Incorrect settings can make a device unreachable or incompatible. Verify actual hardware capabilities.", PALE_RED),
            para("11. File health, comparison, and exports", "h1"),
            *bullets([
                "File health combines statistics, errors, warnings, free/local subscriptions, and compatibility checks.",
                "XML comparison displays differences in a table.",
                "TXT/PDF exports include the application version and the By Mamat et ses agents signature.",
            ]),
        ]
        page5 = [
            para("12. Save and final validation", "h1"),
            para("Use Save as. The temporary XML is reloaded, protected changes are checked, and the destination is replaced atomically. A failure before replacement leaves the previous destination intact."),
            data_table(
                ["Check", "Recommended action"],
                [
                    ["Items to check", "Open affected devices and explain or correct every unexpected difference."],
                    ["Modified only", "Verify that only the intended devices appear."],
                    ["Before / after", "Review every changed setting, channel, and subscription."],
                    ["Dante Controller", "Import the file into a working copy before any field operation."],
                ],
                [48, 122],
            ),
            para("13. Regression tests", "h1"),
            para("The V3.08 suite runs 95 Core and Windows contract tests plus 8 headless Avalonia tests covering technical identities, unknown paths, atomic save, recovery, IPv4 interfaces, subscription aliases, default namespaces, synthetic presets, cumulative batches, matrix gestures, conflicts, rollback, persistence, Easy patch, Device details integration, and Mac identity. GitHub Actions reruns them on Windows and macOS."),
            para("14. Known limitations", "h1"),
            *bullets([
                "No real-time Dante control and no communication with devices.",
                "No Audinate SDK/API and no proprietary protocol bypass.",
                "Compatibility depends on the XML structure; only an official import confirms the final file.",
                "Undo keeps at most 10 states to limit memory use.",
                "The matrix displays only the two selected devices to preserve performance on large presets.",
                "Mac DMGs are ad hoc signed but not notarized; first launch may require right-clicking the application and choosing Open.",
                "The Windows Easy patch tab is not reproduced identically on Mac, which keeps the Avalonia visual patch workshop.",
                "Duplicate Tx names are ambiguous in Dante subscriptions and must be renamed before using Easy patch.",
            ]),
            para("15. Help and information", "h1"),
            para("Quick start and Full guide automatically open the French or English PDF for the active language."),
            callout(f"Public project: {GITHUB}<br/>Credit: By Mamat et ses agents", PALE_GREEN),
        ]

    story: list = []
    for index, page in enumerate([page1, page2, page3, page4, page5]):
        if index:
            story.append(PageBreak())
        story.extend(page)
    build_document(ROOT / f"Notice_DanteConfigEditorV3_{language}.pdf", story)


if __name__ == "__main__":
    for language_code in ("FR", "EN"):
        quick_start(language_code)
        full_guide(language_code)
