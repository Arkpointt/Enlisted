# Steam Workshop Upload Guide for Enlisted

This guide covers how to publish the Enlisted mod to the Steam Workshop for Mount & Blade II: Bannerlord.

## Prerequisites

1. **Steam Account** with Bannerlord (AppID: 261550)
2. **SteamCMD** installed - Download from https://developer.valvesoftware.com/wiki/SteamCMD
3. **Built mod** in your Bannerlord Modules folder
4. **Preview image** (512x512 or 1024x1024 PNG, under 1MB)

## First-Time Setup

### 1. Install SteamCMD

Download and extract SteamCMD to a folder like `C:\SteamCMD\`.

### 2. Prepare Your Mod

Make sure your mod is built and working:

```powershell
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

Your mod files should be in the Bannerlord Modules folder:
```
<Bannerlord Install>\Modules\Enlisted\
├── SubModule.xml
├── bin\Win64_Shipping_Client\Enlisted.dll
├── GUI\
├── ModuleData\
└── ...
```

### 3. Create Preview Image

Replace `preview.png.placeholder` with an actual `preview.png` image:
- Dimensions: 512x512 or 1024x1024 pixels (square)
- Format: PNG
- Size: Under 1MB

### 4. Configure workshop_upload.vdf

Edit `workshop_upload.vdf` and update these paths:

```
"contentfolder" "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted"
"previewfile" "C:\Dev\Enlisted\Enlisted\tools\workshop\preview.png"
```

## Uploading to Steam Workshop

### First Upload (Creating the Workshop Item)

1. Open PowerShell or Command Prompt
2. Navigate to SteamCMD:
   ```powershell
   cd C:\SteamCMD
   ```

3. Login and upload:
   ```powershell
   .\steamcmd.exe +login YOUR_STEAM_USERNAME +workshop_build_item "C:\Dev\Enlisted\Enlisted\tools\workshop\workshop_upload.vdf" +quit
   ```
   
   You'll be prompted for your password and Steam Guard code.

4. **Important**: After the first successful upload, Steam assigns a Workshop Item ID. Note this ID from the output (it looks like a long number, e.g., `1234567890`).

5. Update `workshop_upload.vdf` with your new ID:
   ```
   "publishedfileid" "1234567890"
   ```

### Updating an Existing Workshop Item

1. Make sure `publishedfileid` is set to your Workshop Item ID
2. Update the `changenote` field with your release notes
3. Run the same upload command:
   ```powershell
   .\steamcmd.exe +login YOUR_STEAM_USERNAME +workshop_build_item "C:\Dev\Enlisted\Enlisted\tools\workshop\workshop_upload.vdf" +quit
   ```

## Making Your Mod Public

The VDF is configured with `"visibility" "2"` (Hidden) by default. After testing:

1. Go to your Steam Workshop page: https://steamcommunity.com/sharedfiles/filedetails/?id=YOUR_WORKSHOP_ID
2. Click "Edit title & description"
3. Change visibility from "Hidden" to "Public"

Or update the VDF:
```
"visibility" "0"
```
And re-upload.

## Verification Checklist

Before making public, verify:

- [ ] Mod loads correctly when subscribed via Workshop
- [ ] SubModule.xml is present and valid
- [ ] All required DLLs are included
- [ ] Preview image displays correctly
- [ ] Description renders properly (check Steam formatting)
- [ ] Tags are appropriate
- [ ] Dependencies are listed (Harmony requirement mentioned)

After publishing:

- [ ] Subscribe to your own mod
- [ ] Launch Bannerlord with only Workshop version enabled
- [ ] Test basic functionality (enlist, battle, duties, etc.)
- [ ] Check for any missing files or errors

## Troubleshooting

### "Login Failed"
- Verify Steam credentials
- Check Steam Guard code
- Ensure your account owns Bannerlord

### "Item Update Failed"
- Verify contentfolder path exists
- Check previewfile path is correct
- Ensure preview image is under 1MB
- Verify AppID is 261550

### "Missing Content"
- Make sure SubModule.xml is in the root of contentfolder
- Verify bin folder contains the compiled DLL
- Check file permissions

## File Structure Reference

```
tools/workshop/
├── WORKSHOP_UPLOAD.md      (this file)
├── workshop_upload.vdf      (upload configuration)
├── preview.png              (your workshop thumbnail)
└── preview.png.placeholder  (delete after adding real image)
```

## Quick Reference

| Field | Value |
|-------|-------|
| AppID | 261550 |
| Mod Name | Enlisted |
| Version | v0.5.7 |
| Visibility | 0=Public, 1=Friends, 2=Hidden |

