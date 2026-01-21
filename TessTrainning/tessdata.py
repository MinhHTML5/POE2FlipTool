import os
import subprocess
import shutil
from pathlib import Path

# =true if you want train from scatch
train_new = False
BASE_DIR = Path(__file__).parent.resolve()
DATA_DIR = BASE_DIR / "data"
WORK_DIR = BASE_DIR / "work"
PROC_DIR = BASE_DIR / "proc"
OUTPUT_DIR = WORK_DIR / "output"

TESSDATA_DIR = (BASE_DIR / ".." / "tessdata").resolve()
ENG_TRAINEDDATA = TESSDATA_DIR / "eng.traineddata"

BASELANG = "eng"
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
    PROC_DIR.mkdir(exist_ok=True)
    train_new = True
    if os.path.exists(f"{OUTPUT_DIR}"):
        shutil.rmtree(OUTPUT_DIR)

    # 1) unicharset
    boxes = sorted(WORK_DIR.glob("*.box"))
    run(["unicharset_extractor"] + [str(b) for b in boxes], cwd=WORK_DIR)

    # 2) extract minimal LSTM pieces
    run([
        "combine_tessdata", "-u",
        str(ENG_TRAINEDDATA), "eng"
    ], cwd=PROC_DIR)

    shutil.copy(WORK_DIR / "unicharset", PROC_DIR / f"{LANG}.unicharset")
    shutil.copy(PROC_DIR / f"{BASELANG}.lstm", PROC_DIR / f"{LANG}.lstm")
    shutil.copy(PROC_DIR / f"{BASELANG}.lstm-recoder", PROC_DIR / f"{LANG}.lstm-recoder")
    shutil.copy(PROC_DIR / f"{BASELANG}.lstm-unicharset", PROC_DIR / f"{LANG}.lstm-unicharset")

    # 3) traineddata shell
    run(["combine_tessdata", f"{LANG}."], cwd=PROC_DIR)

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
    if train_new:
        print("Start new training")
        run([
            "lstmtraining",
            # "--continue_from", f"{PROC_DIR}/{BASELANG}.lstm",
            "--net_spec", NET_SPEC,
            "--traineddata", f"{PROC_DIR}/{LANG}.traineddata",
            "--model_output", f"output/{LANG}",
            "--train_listfile", "list.txt",
            "--learning_rate", "0.00005",
            "--max_iterations", "100000",
            # "--debug_interval", "1"
        ], cwd=WORK_DIR)
    else:
        print("Training old model")
        run([
            "lstmtraining",
            "--continue_from", f"{PROC_DIR}/{LANG}.lstm",
            # "--net_spec", NET_SPEC,
            "--traineddata", f"{PROC_DIR}/{LANG}.traineddata",
            "--model_output", f"output/{LANG}",
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
        "--traineddata", f"{PROC_DIR}/{LANG}.traineddata",
        "--model_output", f"{OUTPUT_DIR}/{LANG}.traineddata"
    ], cwd=WORK_DIR)

    
    shutil.copy(f"{OUTPUT_DIR}/{LANG}.traineddata",f"{ENG_TRAINEDDATA}")


if __name__ == "__main__":
    import sys
    cmd = sys.argv[1] if len(sys.argv) > 1 else "all"
    if cmd in ("prep", "all"):
        prep()
    if cmd in ("train", "all"):
        train()
