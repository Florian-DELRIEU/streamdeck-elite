# Builds Elite/ZV Elite.streamDeckProfile, the profile delivered with the plugin for the trial of v3.7 (pages instead
# of folders, docs/L10-v3.md): an empty profile for the Stream Deck MK.2 with 2 pages, that Florian fills himself.
# Format of the profiles exported by the Stream Deck software ("Version": "2.0", as its own DefaultProfiles): a zip
# holding <profile id>.sdProfile/manifest.json (device, name, pages) and Profiles/<page folder>/manifest.json, where
# the page folder is the page id (16 bytes, big endian) in base 32 with the alphabet 0-9 A-T V W, followed by "Z"
# (checked on StreamDeck_winDefault.streamDeckProfile of the Stream Deck software 7.4).
# Nothing is copied from a real profile. The ids are fixed: the file only changes when this script changes.
# Usage (from anywhere): python tools/make-test-profile.py
import json
import os
import uuid
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGET = os.path.join(ROOT, "Elite", "ZV Elite.streamDeckProfile")

NAME = "ZV Elite"
MODEL = "20GBA9901"  # Stream Deck MK.2 (model of the profiles of Florian)
PROFILE = uuid.UUID("5a1f3c2e-7b64-4c1d-9e2a-0b3c4d5e6f70")
DEFAULT_PAGE = uuid.UUID("5a1f3c2e-7b64-4c1d-9e2a-0b3c4d5e6f71")
PAGES = [uuid.UUID("5a1f3c2e-7b64-4c1d-9e2a-0b3c4d5e6f72"), uuid.UUID("5a1f3c2e-7b64-4c1d-9e2a-0b3c4d5e6f73")]

ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTVW"


def page_folder(page):
    bits = "".join(f"{b:08b}" for b in page.bytes)
    bits += "0" * ((5 - len(bits) % 5) % 5)
    return "".join(ALPHABET[int(bits[i:i + 5], 2)] for i in range(0, len(bits), 5)) + "Z"


def empty_page():
    return {"Controllers": [{"Actions": {}, "Type": "Keypad"}], "Icon": "", "Name": ""}


def main():
    root = str(PROFILE).upper() + ".sdProfile"
    manifest = {
        "Device": {"Model": MODEL, "UUID": ""},
        "Name": NAME,
        "Pages": {"Current": str(PAGES[0]), "Default": str(DEFAULT_PAGE), "Pages": [str(p) for p in PAGES]},
        "Version": "2.0",
    }

    # fixed date: the zip is identical from one run to the next
    def add(archive, name, content):
        info = zipfile.ZipInfo(name, date_time=(2026, 9, 28, 0, 0, 0))
        if name.endswith("/"):
            info.compress_type = zipfile.ZIP_STORED
            info.external_attr = (0o40775 << 16) | 0x10  # directory (unix mode and MS-DOS flag)
        else:
            info.compress_type = zipfile.ZIP_DEFLATED
        archive.writestr(info, content)

    # folders are listed as entries too, as in the profiles of the Stream Deck software
    with zipfile.ZipFile(TARGET, "w") as archive:
        add(archive, root + "/", "")
        add(archive, root + "/manifest.json", json.dumps(manifest, indent=4))
        add(archive, root + "/Profiles/", "")
        for page in [DEFAULT_PAGE] + PAGES:
            folder = root + "/Profiles/" + page_folder(page) + "/"
            add(archive, folder, "")
            add(archive, folder + "manifest.json", json.dumps(empty_page(), indent=4))

    print("written:", os.path.relpath(TARGET, ROOT), os.path.getsize(TARGET), "bytes")


if __name__ == "__main__":
    main()
