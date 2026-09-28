# Cloud pre-build (Linux + Mono, docs/feuille-de-route-v3.md "Compiler dans le cloud"): restores the NuGet packages of
# the packages.config files from nuget.org, then compiles the classic csproj of the solution with Roslyn (csc.exe of
# Microsoft.Net.Compilers.Toolset, run by mono), C# 7.3, against the Mono 4.8 reference assemblies, and runs
# Elite.CatalogGen (catalog.js / commands.js). Not a replacement for build.ps1 on Windows.
# Prerequisite: apt-get install -y mono-complete. Usage: python3 tools/cloud/cloudbuild.py
import os, re, shutil, subprocess, sys, glob, zipfile, urllib.request
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NS = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
ROSLYN = ('Microsoft.Net.Compilers.Toolset', '4.8.0')
API = '/usr/lib/mono/4.8-api'

PROJECTS = [
    ('InputSimulatorPlus/WindowsInput/WindowsInput.csproj', 'InputSimulatorPlus/WindowsInput/bin/Debug'),
    ('EliteJournalReader/EliteJournalReader.csproj', 'EliteJournalReader/bin/Debug'),
    ('Elite.CatalogGen/Elite.CatalogGen.csproj', 'Elite.CatalogGen/bin/Debug'),
    ('Elite/Elite.csproj', 'Elite/bin/Debug/com.mhwlng.elite.sdPlugin'),
    ('Elite.Tests/Elite.Tests.csproj', 'Elite.Tests/bin/Debug'),
]


def restore():
    """Downloads the packages of every packages.config (and Roslyn) into packages/<Id>.<Version>, like nuget restore."""
    wanted = {ROSLYN}
    for config in glob.glob(os.path.join(ROOT, '*', 'packages.config')):
        if 'InputSimulatorPlus' in config:
            continue
        for m in re.finditer(r'id="([^"]+)" version="([^"]+)"', open(config, encoding='utf-8-sig').read()):
            wanted.add(m.groups())
    for pid, ver in sorted(wanted):
        dest = os.path.join(ROOT, 'packages', f'{pid}.{ver}')
        if os.path.isdir(dest):
            continue
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        url = f'https://api.nuget.org/v3-flatcontainer/{pid.lower()}/{ver.lower()}/{pid.lower()}.{ver.lower()}.nupkg'
        archive = dest + '.nupkg'
        urllib.request.urlretrieve(url, archive)
        zipfile.ZipFile(archive).extractall(dest)
        os.remove(archive)
        print(f'restored {pid} {ver}')


def norm(p):
    return p.replace('\\', '/')


def build(csproj, outdir, outputs):
    projdir = os.path.dirname(os.path.join(ROOT, csproj))
    tree = ET.parse(os.path.join(ROOT, csproj)).getroot()
    name = tree.find('.//m:AssemblyName', NS).text
    kind = tree.find('.//m:OutputType', NS).text
    ext = '.exe' if kind.lower() in ('exe', 'winexe') else '.dll'
    out = os.path.join(ROOT, outdir)
    os.makedirs(out, exist_ok=True)

    sources = [os.path.normpath(os.path.join(projdir, norm(c.get('Include')))) for c in tree.findall('.//m:Compile', NS)]
    refs = [API + '/mscorlib.dll', API + '/System.Core.dll'] + glob.glob(API + '/Facades/*.dll')
    for r in tree.findall('.//m:Reference', NS):
        rname = r.get('Include').split(',')[0].strip()
        hint = r.find('m:HintPath', NS)
        if hint is not None:
            path = os.path.normpath(os.path.join(projdir, norm(hint.text)))
            refs.append(path)
            shutil.copy2(path, out)
        elif os.path.exists(f'{API}/{rname}.dll'):
            refs.append(f'{API}/{rname}.dll')
        else:
            print(f'  ({name}: framework reference {rname} not found, skipped)')
    for pr in tree.findall('.//m:ProjectReference', NS):
        roa = pr.find('m:ReferenceOutputAssembly', NS)
        if roa is not None and roa.text.strip().lower() == 'false':
            continue
        target = os.path.normpath(os.path.join(projdir, norm(pr.get('Include'))))
        built = outputs[target]
        refs.append(built)
        for f in glob.glob(os.path.join(os.path.dirname(built), '*.dll')) + [built]:
            shutil.copy2(f, out)

    unsafe = tree.find('.//m:AllowUnsafeBlocks', NS)
    target_path = os.path.join(out, name + ext)
    csc = os.path.join(ROOT, 'packages', f'{ROSLYN[0]}.{ROSLYN[1]}', 'tasks', 'net472', 'csc.exe')
    args = ['mono', csc, '/nologo', '/noconfig', '/nostdlib+', '/langversion:7.3', '/codepage:65001', '/debug:portable', '/define:DEBUG;TRACE',
            '/target:' + ('exe' if ext == '.exe' else 'library'), '/out:' + target_path, '/warn:0']
    if unsafe is not None and unsafe.text.strip().lower() == 'true':
        args.append('/unsafe+')
    args += ['/r:' + r for r in dict.fromkeys(refs)] + sources
    res = subprocess.run(args, capture_output=True, text=True)
    errors = [l for l in (res.stdout + res.stderr).splitlines() if 'error' in l.lower()]
    print(f'{name}{ext}: ' + ('OK' if res.returncode == 0 else f'FAILED ({len(errors)} errors)'))
    for l in errors[:40]:
        print('   ', l.replace(ROOT + '/', ''))
    if res.returncode != 0:
        sys.exit(1)

    # content / none items copied to the output directory
    for item in tree.findall('.//m:Content', NS) + tree.findall('.//m:None', NS):
        copy = item.find('m:CopyToOutputDirectory', NS)
        if copy is None or copy.text.strip() == 'Never':
            continue
        src = os.path.normpath(os.path.join(projdir, norm(item.get('Include'))))
        link = item.find('m:Link', NS)
        rel = norm(link.text) if link is not None else norm(item.get('Include'))
        dst = os.path.join(out, rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        if os.path.exists(src):
            shutil.copy2(src, dst)
    outputs[os.path.join(ROOT, csproj)] = target_path
    return target_path


def main():
    restore()
    outputs = {}
    for csproj, outdir in PROJECTS:
        build(csproj, outdir, outputs)
        if csproj.startswith('Elite.CatalogGen'):
            res = subprocess.run(['mono', outputs[os.path.join(ROOT, csproj)], 'generate', ROOT], capture_output=True, text=True)
            print('  ' + (res.stdout + res.stderr).strip().replace('\n', '\n  '))
            if res.returncode != 0:
                sys.exit(1)
    print('== CLOUD BUILD OK')


if __name__ == '__main__':
    main()
