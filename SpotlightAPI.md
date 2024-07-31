# Spotlight API

This page holds details about the Spotlight API. Example implementation can be found in [Spotlight.cs](SpotlightDownloader/Spotlight.cs).

Both APIs are hosted on the same infrastructure, which can be requested at `arc.msn.com` or `fd.api.iris.microsoft.com` using the endpoint described below.

## API v3

- Used by: Windows 10
- Used for: Lockscreen
- Default hostname: `arc.msn.com`
- Endpoint: `/v3/Delivery/Placement`
- Maximum resolution: `1080p`
- File names: No
- Metadata: **Yes**
- Server-side scaling: **Yes**
- Hashes for integrity: **Yes**

### Sample request

```
https://arc.msn.com/v3/Delivery/Placement?pid=209567&fmt=json&rafb=0&ua=WindowsShellClient%2F0&cdm=1&disphorzres=9999&dispvertres=9999&lo=80217&pl=en-US&lc=en-US&ctry=us&time=2017-12-31T23:59:59Z
```

Where the expected arguments are:
- `pid`: Public subscription ID for Windows lockscreens. Do not change this value
- `fmt`: Output format, e.g. `json`
- `rafb`: Purpose currently unknown, optional
- `ua`: Client user agent string
- `cdm`: Purpose currently unknown, `cdm=1`
- `disphorzres`: Screen width in pixels
- `dispvertres`: Screen height in pixels
- `lo`: Purpose currently unknown, optional
- `pl`: Locale, e.g. `en-US`
- `lc`: Language, e.g. `en-US`
- `ctry`: Country, e.g. `us`
- `time`: Time, e.g. `2017-12-31T23:59:59Z`

The JSON response contains details for one or more image(s) including image url, title, sha256, ads, etc.

**Credits:** APIv3 URL was originally found in this [file](https://github.com/KoalaBR/spotlight/blob/3164a43684dcadb751ce9a38db59f29453acf2fe/spotlightprovider.cpp#L17) and up to date API URL in this [file](https://github.com/Biswa96/WinLight/blob/master/Developers.md), thanks to the authors for their findings!

## API v4

- Used by: Windows 10
- Used for: Lockscreen Lockscreen, Wallpaper
- Default hostname: `fd.api.iris.microsoft.com`
- Endpoint: `/v4/api/selection`
- Maximum resolution: `4K`
- File names: Partial (for some images)
- Metadata: **Yes**
- Server-side scaling: **No**
- Hashes for integrity: **No**

### Sample request

Sample Raw request with a few redacted fields:

```
https://fd.api.iris.microsoft.com/v4/api/selection?&asid=3EB**************************4E4&nct=1&placement=88000820&bcnt=4&country=US&locale=en-US&poptin=0&fmt=json&clr=cdmlite&MX_FlightIds=MD%3A2************************************************A2A&arch=AMD64&concp=0&d3dfl=D3D_FEATURE_LEVEL_10_1&devfam=Windows.Desktop&devosver=10.0.22631.3880&dinst=172****261&dmret=0&drgng=84&flightbranch=&flightring=Retail&iepe=2&iste=2&localid=w%3A6B******-****-****-****-*********C86&oem=VMware7%2C1&oemn=VMware%2C%20Inc.&oemname=VMware%2C%20Inc.&osbranch=ni_release&oslocale=fr-FR&osret=1&ossku=Enterprise&osskuid=4&prccn=4&prccs=2395&prcmf=GenuineIntel&procm=Intel%28R%29%20Core%28TM%29%20i7-******%20CPU%20%40%20*.**GHz&ram=4096&smbiosdm=VMware7%2C1&tinst=Client&tl=0&usri=-2&pat=0&smc=0&sac=0&disphorzres=1920&dispsize=23.0&dispvertres=1080&ldisphorzres=1920&ldispvertres=1080&moncnt=1&cpdsk=1****9&frdsk=7***0&lo=1*****9&tsu=3**1&app=desktop
```

This API version takes a lot of analytics parameters that have no useful effect on the API response, so the API call can be simplifed as follows:

```
https://fd.api.iris.microsoft.com/v4/api/selection?&placement=88000820&bcnt=4&country=US&locale=en-US&fmt=json
```

- `placement`: Public subscription ID for Windows lockscreens and wallpapers. Do not change this value
- `bcnt`: Amount of pictures to return. Allowed range: `1` to `4`.
- `country`: Country, e.g. `US`. Different images are returned for different countries.
- `locale`: Locale, e.g. `en-US`. Different descriptions are returned for different locales. Use a locale that is consistent with the chosen country.
- `fmt`: Format of the response. Only JSON is supported, and omitting this parameter will return JSON regardless.

Note: The `disphorzres` and `dispvertres` parameters are analytic values that have no effect on this API version.

**Credits:** APIv4 analysis conducted by ORelio for the Spotlight Downloader project. Endpoint found through analysis of network trafic originating from a Windows 11 virtual machine. Please credit the Spotlight Downloader project if you use this documentation for your own project.
