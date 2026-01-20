import subprocess
import shutil
from pathlib import Path

BASE_DIR = Path(__file__).parent.resolve()
DATA_DIR = BASE_DIR / "data"
WORK_DIR = BASE_DIR / "work"
OUTPUT_DIR = WORK_DIR / "output"

TESSDATA_DIR = (BASE_DIR / ".." / "tessdata").resolve()
ENG_TRAINEDDATA = TESSDATA_DIR / "eng.traineddata"

LANG = "num"
PSM = "7"

NET_SPEC = "[1,36,0,1Ct3,3,16Mp3,3Lfys64Lfx96Lrx96Lfx256O1c111]"


def run(cmd, cwd=None):
    print("[RUN]", " ".join(map(str, cmd)))
    subprocess.run(cmd, check=True, cwd=cwd)


# ---------------- PREP (BOX ONLY) ----------------
def prep():
    WORK_DIR.mkdir(exist_ok=True)

    for img in DATA_DIR.glob("*.*"):
        name = img.stem
        tif = WORK_DIR / f"{name}.tif"
        shutil.copy(img, tif)

        run([
            "tesseract", str(tif), str(WORK_DIR / name),
            "--psm", PSM,
            # "--oem", "1",
            "batch.nochop", 
            "makebox"
        ], cwd=WORK_DIR)


# ---------------- TRAIN ----------------
def train():
    WORK_DIR.mkdir(exist_ok=True)

    # 1) unicharset
    boxes = sorted(WORK_DIR.glob("*.box"))
    run(["unicharset_extractor"] + [str(b) for b in boxes], cwd=WORK_DIR)

    # 2) extract minimal LSTM pieces
    run([
        "combine_tessdata", "-u",
        str(ENG_TRAINEDDATA), "eng"
    ], cwd=WORK_DIR)

    shutil.copy(WORK_DIR / "unicharset", WORK_DIR / f"{LANG}.unicharset")
    shutil.copy(WORK_DIR / "eng.lstm", WORK_DIR / f"{LANG}.lstm")
    shutil.copy(WORK_DIR / "eng.lstm-recoder", WORK_DIR / f"{LANG}.lstm-recoder")
    shutil.copy(WORK_DIR / "eng.lstm-unicharset", WORK_DIR / f"{LANG}.lstm-unicharset")

    # 3) traineddata shell
    run(["combine_tessdata", f"{LANG}."], cwd=WORK_DIR)

    # 4) REGENERATE LSTMF WITH *NUM.TRAINEDDATA*
    for f in WORK_DIR.glob("*.lstmf"):
        f.unlink()

    lstmf = []
    for tif in WORK_DIR.glob("*.tif"):
        name = tif.stem
        run([
            "tesseract", str(tif), str(WORK_DIR / name),
            "--psm", PSM, 
            # "--oem", "1",
            # "-l", "num",
            "lstm.train",
            # "--traineddata", f"{LANG}.traineddata"
        ], cwd=WORK_DIR)
        lstmf.append(f"{name}.lstmf")

    (WORK_DIR / "list.txt").write_text(
        "\n".join(lstmf),
        encoding="utf-8",
        newline="\n"   # FORCE UNIX EOL
    )

    OUTPUT_DIR.mkdir(exist_ok=True)
    # 5) train
    run([
        "lstmtraining",
        "--net_spec", NET_SPEC,
        "--traineddata", f"{LANG}.traineddata",
        "--model_output", "output/num",
        "--train_listfile", "list.txt",
        "--learning_rate", "0.00005",
        "--max_iterations", "100000",
        # "--debug_interval", "1"
    ], cwd=WORK_DIR)

    # 6) finalize
    run([
        "lstmtraining",
        "--stop_training",
        "--continue_from", f"{OUTPUT_DIR}/{LANG}_checkpoint",
        "--traineddata", f"{LANG}.traineddata",
        "--model_output", f"{OUTPUT_DIR}/{LANG}.traineddata"
    ], cwd=WORK_DIR)


if __name__ == "__main__":
    import sys
    cmd = sys.argv[1] if len(sys.argv) > 1 else "all"
    if cmd in ("prep", "all"):
        prep()
    if cmd in ("train", "all"):
        train()
