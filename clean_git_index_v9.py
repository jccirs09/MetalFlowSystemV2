import os
import shutil
import subprocess

def clean_git_index():
    # 1. Identify "bin\" directories
    if os.path.exists("MetalFlowSystemV2/bin\\Debug"):
         shutil.rmtree("MetalFlowSystemV2/bin\\Debug")
         print("Deleted MetalFlowSystemV2/bin\\Debug from FS")

    # Now git rm
    try:
        subprocess.run(["git", "rm", "-r", "--cached", "."], check=True)
        subprocess.run(["git", "add", "."], check=True)
    except Exception as e:
        print(f"Error during git ops: {e}")

    # Now check status
    status = subprocess.run(["git", "status"], capture_output=True, text=True).stdout
    print(status)

if __name__ == "__main__":
    clean_git_index()
