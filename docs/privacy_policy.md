# Privacy Policy - Game Macro Assistant

**Last Updated:** January 2025  
**Version:** 1.0

## Overview

Game Macro Assistant ("the Application") is designed to respect your privacy while providing macro automation functionality. This policy explains what data we collect, how it's used, and your rights regarding your information.

## Data We Collect

### Automatically Collected Data

**Crash Reports (R-019)**
- Application crash dumps and stack traces
- System information (OS version, hardware configuration)
- Application version and configuration
- Timestamp and circumstances leading to crash
- **Storage Location:** `%LOCALAPPDATA%\GameMacroAssistant\CrashDumps\`

**Performance Metrics**
- CPU and memory usage statistics
- Timing accuracy measurements
- Feature usage patterns (anonymized)

**Error Logs**
- Error codes and diagnostic information
- File operation results
- **Storage Location:** `%APPDATA%\GameMacroAssistant\Logs\`

### User-Created Data

**Macro Files**
- Mouse and keyboard sequences you record
- Screenshots captured during macro recording
- Custom settings and configurations
- **Storage Location:** User-selected directories
- **Encryption:** Optional, with user-controlled passphrases

**Settings and Preferences**
- UI preferences and window layouts
- Hotkey assignments
- Image matching thresholds

## How We Use Your Data

### Local Processing Only
- All macro execution happens locally on your device
- Screenshots and recorded inputs never leave your computer
- No cloud processing or external servers involved

### Crash Report Submission
Crash reports are only sent if you explicitly consent. You will be prompted with:

> **"Help Improve Game Macro Assistant"**
> 
> The application has encountered an unexpected error. Would you like to send a crash report to help us improve the software?
> 
> **What's included:**
> - Technical details about the crash
> - System configuration information
> - Application logs (no personal macros or screenshots)
> 
> **What's NOT included:**
> - Your recorded macros or screenshots
> - Personal files or data
> - Browsing history or other applications
> 
> [Send Report] [Don't Send] [More Details]

### Crash Report Destination
**Submission URL:** `https://crash-reports.gamemacroassistant.com/api/v1/reports`

**Data Handling:**
- Reports are used solely for debugging and software improvement
- No personal identification or tracking
- Automatic deletion after 90 days
- Secure transmission via HTTPS

## Data We Don't Collect

- **Personal Identity:** No names, emails, or contact information
- **Browsing History:** No web activity monitoring
- **File Contents:** No access to files outside the application directory
- **Screenshots:** Only captured during macro recording, stored locally
- **Network Activity:** No monitoring of your internet usage
- **Game Data:** No access to game save files or progression

## Your Rights and Control

### Data Control
- **Opt-out:** Crash reporting is entirely optional
- **Local Storage:** All your macros remain on your device
- **Deletion:** Uninstalling removes all application data
- **Encryption:** Protect sensitive macros with passphrases

### Accessing Your Data
Your data locations:
```
Macros:           [User-selected directories]
Settings:         %APPDATA%\GameMacroAssistant\Settings\
Logs:            %APPDATA%\GameMacroAssistant\Logs\
Crash Dumps:     %LOCALAPPDATA%\GameMacroAssistant\CrashDumps\
```

### Data Retention
- **Logs:** Automatically rotate after 30 days
- **Crash Dumps:** Kept locally until manually deleted
- **Macros:** Retained until you delete them
- **Settings:** Retained until application uninstall

## Security Measures

### Local Security
- Optional macro encryption with user-controlled passphrases
- Secure storage of sensitive settings
- No network-based authentication or accounts

### Transmission Security
- Crash reports sent via HTTPS with certificate validation
- No sensitive macro data included in reports
- Minimal system information for debugging purposes

## Third-Party Services

### None Currently Used
Game Macro Assistant does not integrate with:
- Cloud storage services
- Social media platforms
- Analytics services
- Advertising networks
- Third-party authentication providers

### Future Integrations
Any future third-party integrations will:
- Require explicit user consent
- Be clearly documented in policy updates
- Maintain the same privacy standards
- Provide opt-out options

## Children's Privacy

Game Macro Assistant is not specifically designed for children under 13. We do not knowingly collect personal information from children. If you believe a child has provided information to us, please contact us immediately.

## Changes to This Policy

### Notification Process
- Policy updates will be included in application updates
- Material changes will be highlighted in update notes
- Continued use of the application constitutes acceptance
- Previous versions available in application documentation

### Version History
- **v1.0 (January 2025):** Initial privacy policy

## Contact Information

### Questions or Concerns
For privacy-related questions, contact:
- **Email:** privacy@gamemacroassistant.com
- **GitHub Issues:** https://github.com/gamemacroassistant/issues
- **Documentation:** https://docs.gamemacroassistant.com/privacy

### Data Subject Requests
To request information about data we may have:
1. Check your local directories (listed above)
2. Contact us with specific questions
3. Note: Most data is stored locally on your device

## Compliance

### Standards
This application and policy comply with:
- General privacy best practices
- Windows application security guidelines
- Open source software standards

### Jurisdiction
This policy is governed by the laws of the jurisdiction where the software is developed and distributed.

---

**Remember:** Your macros, screenshots, and recorded sequences always remain on your local device unless you explicitly choose to share them. Game Macro Assistant is designed to be a privacy-respecting, local-first application.