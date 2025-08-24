# ScreenScraper Integration

This document describes the ScreenScraper.fr API integration added to PSXPackagerPlus.

## Features

The ScreenScraper integration allows you to automatically fetch game information and artwork for your PlayStation games using the ScreenScraper.fr database.

### What's Included

1. **Game Information Retrieval**: Automatically fetch game metadata including:
   - Game name
   - Publisher and developer information
   - Release dates (prioritizing US, falling back to JP)
   - Synopsis/description (prioritizing English, falling back to Japanese)
   - Player count
   - Rating information

2. **Artwork Download**: Download various types of game artwork:
   - Screenshots
   - Box art (US and JP regions)
   - Wheel logos (US and JP regions)
   - And more media types

3. **Automatic File Identification**: Uses file hashes (MD5, SHA1, CRC32) to accurately identify games

## How to Use

### 1. Get ScreenScraper Credentials

First, you need to obtain developer credentials from ScreenScraper.fr:
- Visit https://www.screenscraper.fr/
- Register for an account
- Request developer access to get your Developer ID and Developer Password

### 2. Configure Credentials

In the PSXPackagerGUI:
1. Open a game in Single Mode
2. Click on the "ScreenScraper" tab
3. Enter your credentials:
   - **Developer ID**: Your ScreenScraper developer ID
   - **Dev Password**: Your ScreenScraper developer password
   - **Username**: (Optional) Your ScreenScraper username for higher API limits
   - **Password**: (Optional) Your ScreenScraper password

### 3. Search for Game Information

1. Load a disc/ISO file in the Discs section
2. Go to the ScreenScraper tab
3. Click "Search Game Info"
4. The system will:
   - Calculate file hashes (MD5, SHA1, CRC32)
   - Query the ScreenScraper database
   - Display found game information

### 4. Download Artwork

Once game information is found:
1. Browse the available media in the "Game Media" section
2. Click "Download" next to any artwork you want to use
3. The artwork will be automatically set as the PIC0 (background image) resource

## Technical Details

### API Integration

- Uses ScreenScraper API v2
- Targets PlayStation system (ID: 57)
- Prioritizes English language and US region data
- Falls back to Japanese language and region if US/English not available
- Implements proper error handling and timeout management

### File Hash Calculation

The integration calculates three types of hashes for accurate game identification:
- **MD5**: Standard MD5 hash of the entire file
- **SHA1**: SHA1 hash of the entire file  
- **CRC32**: CRC32 checksum of the entire file

### Supported File Types

- ISO files
- BIN files (with or without CUE sheets)
- Any file format supported by the main PSXPackager application

## Code Structure

### New Files Added

1. **PSXPackager.Common/ScreenScraper/**
   - `ScreenScraperService.cs`: Main API service class
   - `GameInfo.cs`: Data models for API responses

2. **PSXPackagerGUI/Models/**
   - `ScreenScraperModel.cs`: View model for ScreenScraper functionality

3. **PSXPackagerGUI/Converters/**
   - `NullToVisibilityConverter.cs`: UI converter for showing/hiding elements

### Modified Files

1. **SinglePage.xaml**: Added ScreenScraper tab with UI controls
2. **SinglePage.xaml.cs**: Added password box event handlers
3. **SingleModel.cs**: Integrated ScreenScraperModel
4. **Disc.cs**: Added Path property for file location
5. **ResourceModel.cs**: Added IsEmpty property
6. **PSXPackagerGUI.csproj**: Added reference to PSXPackager.Common

## API Rate Limits

ScreenScraper has API rate limits that vary based on your account type:
- Free accounts: Limited requests per day
- Contributing members: Higher limits
- Registered users with username/password: Additional threads and higher limits

For best results, provide both developer credentials and user credentials.

## Error Handling

The integration includes comprehensive error handling for:
- Network connectivity issues
- API rate limiting
- Invalid credentials
- File not found errors
- Unsupported file formats
- Image download failures

## Future Enhancements

Potential improvements that could be added:
1. Credential persistence in application settings
2. Multiple image download and selection
3. Batch processing for multiple games
4. Custom artwork assignment (ICON0, ICON1, PIC1, etc.)
5. Game information editing before applying
6. Support for other gaming systems