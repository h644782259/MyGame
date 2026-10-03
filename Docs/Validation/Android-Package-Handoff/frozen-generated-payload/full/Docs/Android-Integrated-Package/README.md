# Android integrated package provenance

See source-manifest.json for exact source commits and platform boundaries, payload-file-manifest.json for hashes (excludes this metadata directory), platform-differences.json, guid-audit.json, source-common-hash-audit.json, compact-excluded-review-media.json and blend-dependencies.json. Both archives contain these records. Compact omissions are pure offline review images/videos; runtime and editable files are retained. ZIP hashes remain external to avoid circular self-hashing. No Android commit/push/build/device acceptance.

Offline reproducibility requires local Git repositories containing the cited commits plus Blender. From this directory run, replacing repository paths and using a fresh output directory:

```sh
python3 build_android_integrated.py --win-repo /path/to/windows --ios-repo /path/to/ios --win-head 5d85e47b9fab2489d5b06963a0b896ec19112740 --ios-head d3a4aab185eb368f5a4a7aba67b43678bfea508f --android-base 8654c803eb29b87dea7d69f09678ec21e1955f6d --win-base 25096ba7dbf6e9d7ba9463ec29104c2c462da426 --output /path/to/new-output
```
