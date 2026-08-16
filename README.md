# S1API - A Schedule One Mono / Il2Cpp Cross Compatibility Layer

[![IL2CPP Build Check](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml/badge.svg?branch=stable)](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml)
[![Build and Deploy Documentation](https://github.com/ifBars/S1API/actions/workflows/docs.yml/badge.svg)](https://github.com/ifBars/S1API/actions/workflows/docs.yml)
[![GitHub stars](https://img.shields.io/github/stars/ifBars/S1API)](https://github.com/ifBars/S1API/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/ifBars/S1API)](https://github.com/ifBars/S1API/network/members)
[![GitHub issues](https://img.shields.io/github/issues/ifBars/S1API)](https://github.com/ifBars/S1API/issues)
[![GitHub license](https://img.shields.io/github/license/ifBars/S1API)](https://github.com/ifBars/S1API/blob/stable/LICENSE)
[![GitHub last commit](https://img.shields.io/github/last-commit/ifBars/S1API/stable)](https://github.com/ifBars/S1API/commits/stable)
[![Context7 llms.txt](https://img.shields.io/badge/Context7-llms.txt-blue?logo=data%3Aimage%2Fx-icon%3Bbase64%2CiVBORw0KGgoAAAANSUhEUgAAAEoAAABLCAYAAADXjBHUAAAACXBIWXMAAAsTAAALEwEAmpwYAAABaWlDQ1BEaXNwbGF5IFAzAAB4nHWQvUvDUBTFT6tS0DqIDh0cMolD1NIKdnFoKxRFMFQFq1OafgltfCQpUnETVyn4H1jBWXCwiFRwcXAQRAcR3Zw6KbhoeN6XVNoi3sfl%2FTicc7lcwBtQGSv2AijplpFMxKS11Lrke4OHnlOqZrKooiwK%2Fv276%2FPR9d5PiFlNu3YQ2U9cl84ul3aeAlN%2F%2FV3Vn8maGv3f1EGNGRbgkYmVbYsJ3iUeMWgp4qrgvMvHgtMunzuelWSc%2BJZY0gpqhrhJLKc79HwHl4plrbWD2N6f1VeXxRzqUcxhEyYYilBRgQQF4X%2F8044%2Fji1yV2BQLo8CLMpESRETssTz0KFhEjJxCEHqkLhz634PrfvJbW3vFZhtcM4v2tpCAzidoZPV29p4BBgaAG7qTDVUR%2Bqh9uZywPsJMJgChu8os2HmwiF3e38M6Hvh%2FGMM8B0CdpXzryPO7RqFn4Er%2FQcXKWq8MSlPPgAABA5JREFUeAHtmzFME2EUgB%2BGQXCgGCSBpeiICRjERROKoYtG4kANC6kDasLCAgNLO%2BBcBxgYxIHiZhkgRJcahcgmJB1k1HaBBBLtomHTezXVu%2F%2Bu9N3de03Q9yUM%2FcPlb7%2F%2B79279%2F9tap4bfwdKXZqtv2FQ6nIOFBIqioiKIqKiiKgoIiqKiIoioqKIqCgiKoqIiiKioog0Q0hil3th%2BcEU%2BGX78z5M5pZ8XZObmIX%2B7ij4JbGagcJhEcIQWhTS034J%2FFIKcE2kpTXQXHhdWDT0iKgoIqFDr3BYguK3Y0dIlKzXK7tbp16H%2F%2BOX7N4WbFm57TSS12Ou9xJkLpPQoson3%2BGRlZTzj9N%2FxqLWGy2f%2FIDFndfASbaO%2FNRIwpXDxl5mKl9kWFhCD7%2Flmc2sY%2BzZvSTErvRCo%2Bjv7oF0POEYe%2Fo2B4WDInDAlqMWrNVjhsWLxBREzl8AaXCO3MSMY2x9%2FyPM53PABWsyx7oIQ64KhmAqPgbS4BxmyM1urgAnrKIwaS58cOal6Vt3ob%2BrB6TALwPnsIMhx5GX7LCXB4s7bxyrCslY%2BUqK9EjCNVYv6QeBXRTeBbO77x1jmNQlVhWuJiwH7KAk7tWEiBScG1YiNUkODAE3nqtpj381ISKiCgclV%2FglB4eBmyGP8qNeQRoUEVEYfqWvR46xyPlW1roKuxbmnU5KEiL2rIePNiZ9jHmqzyowTUoCuamKmCivhHqty38vqRa4oihzciEmqlR2v%2BkgTbdaePWYzuSK8oLzcSYaoIEXhoaKamvhExVpkX%2BGtNPUPDf%2BE5S6aIeTiIoioqKIqCgiKooIywaoF1jneFXPXE%2F3yYGYa2z7y75YdS4mCh%2BAsWduBytnLlGpuHvHZfLVkiXqDLVZ%2FkXERLV5PK5whgX2vFxzClbrYqJ62jtcY1x7bEipfOSeM9IBUoiJikY6XWOYbLnw6ndFL3aCFGKivFoqXh8uKBuf3H15zn6XiYgoLA282rScOQrbzWbrtzJvRKb9IiLKqzcusde27rHbM3r1BkggImr65h3Ha876yc6qJd%2Fc7bnfOwgSsIvCarzfaPxzHpawg%2BFnbuHjapY4RcMuytw%2Bl1pNVXAL38x9KY%2BN0bCwisLt7UatpirVg2x2cEV5PQuGgU1U5YiP8U3iiTvJ1VQF737m6b7M6MNAJ4hrwSZqbWLGdXZyPr8GjQLnsocg7kwvJ%2Fyff68FiyhcSfaQQ0nx5%2FOVsGgUONfvOf%2FeBTEEuY4chd6Fwbtc%2Fkna93UYLvjB%2FIAHaoPc0QYX5kL%2FcuG%2FaLPoLxcaiIoiEroVXCwfB6qVpH65wDWXiW6pE9HQI6KiiKgoIiqKiIoioqKIqCgiKoqIiiKiooioKCIqiojVPWi6DUpdfgE8wFukKedIwQAAAABJRU5ErkJggg%3D%3D)](http://context7.com/ifbars/s1api/llms.txt)
[![MLVScan Attestation](https://mlvscan.com/attestations/att_RYtq7jrbUY0fpmy0NXJc8UUX/badge.svg?corners=flat)](https://mlvscan.com/attestations/att_RYtq7jrbUY0fpmy0NXJc8UUX)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/ifBars/S1API)

S1API is an open source collaboration project to help standardize Schedule One modding processes.
The goal is to provide a standard place for common functionalities so you can focus on making content versus reverse engineering the game.

## Coverage History

Track S1API's progress in wrapping Schedule One's game types:

[![API Coverage](https://img.shields.io/badge/API%20Coverage-36.8%25-orange)](https://github.com/ifBars/S1API/actions/workflows/coverage.yml)

![Coverage Chart](https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%222025-12-30%22%2C%222025-12-30%22%2C%222026-01-02%22%2C%222026-01-02%22%2C%222026-01-03%22%2C%222026-01-08%22%2C%222026-01-08%22%2C%222026-01-11%22%2C%222026-01-27%22%2C%222026-01-28%22%2C%222026-01-28%22%2C%222026-01-29%22%2C%222026-02-03%22%2C%222026-02-10%22%2C%222026-02-20%22%2C%222026-04-02%22%2C%222026-06-04%22%2C%222026-06-09%22%2C%222026-06-21%22%2C%222026-07-06%22%2C%222026-08-01%22%2C%222026-08-06%22%2C%222026-08-08%22%2C%222026-08-09%22%2C%222026-08-14%22%2C%222026-08-16%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22Class%20Coverage%20%25%22%2C%22data%22%3A%5B25%2C27.491785323110623%2C28.039430449069002%2C28.148959474260675%2C28.258488499452355%2C27.854855923159015%2C27.628865979381445%2C27.938144329896907%2C28.24742268041237%2C28.45360824742268%2C29.07216494845361%2C29.175257731958766%2C29.303278688524593%2C29.815573770491806%2C30.43032786885246%2C30.417495029821072%2C30.61630218687873%2C30.91451292246521%2C31.21272365805169%2C31.610337972166995%2C32.01376936316696%2C32.44406196213425%2C35.03649635036496%2C35.21897810218978%2C35.583941605839414%2C36.77007299270073%5D%2C%22borderColor%22%3A%22rgb%2875%2C%20192%2C%20192%29%22%2C%22backgroundColor%22%3A%22rgba%2875%2C%20192%2C%20192%2C%200.1%29%22%2C%22fill%22%3Afalse%2C%22tension%22%3A0.1%7D%5D%7D%2C%22options%22%3A%7B%22responsive%22%3Atrue%2C%22plugins%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22S1API%20Coverage%20Over%20Time%22%7D%2C%22legend%22%3A%7B%22display%22%3Atrue%2C%22position%22%3A%22top%22%7D%7D%2C%22scales%22%3A%7B%22y%22%3A%7B%22beginAtZero%22%3Atrue%2C%22max%22%3A100%2C%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22Coverage%20%25%22%7D%7D%2C%22x%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22Date%22%7D%7D%7D%7D%7D&width=800&height=400)

*View detailed coverage reports in the [Coverage Analysis workflow](https://github.com/ifBars/S1API/actions/workflows/coverage.yml)*

## Release Channels

S1API is available through multiple release channels:

### **Stable Releases** (Recommended)
The recommended way to get S1API is through official releases:
- **[GitHub Releases](https://github.com/ifBars/S1API/releases)** - Tagged releases with changelogs
- **[Nexus Mods](https://www.nexusmods.com/schedule1/mods/1194)** - Mod repository with version tracking
- **[Thunderstore](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/)** - Mod manager integration

These releases are thoroughly tested and recommended for both mod users and developers.

### **Experimental/Bleeding Edge Builds**
For mod developers or users seeking the latest features and fixes before official releases, experimental builds are automatically generated via GitHub Actions:
- **IL2CPP Build**: Available as artifacts from the [IL2CPP Build Check workflow](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml)
- **Mono Build**: Available as artifacts from the [Documentation workflow](https://github.com/ifBars/S1API/actions/workflows/docs.yml)

⚠️ **Note**: These artifacts represent the bleeding edge of development and may contain untested features or bugs. Use at your own risk. Stable releases remain the recommended choice for most users.

## What Does it Do?
* Allows creation of new game elements (quests, npcs, etc.)
* Provides an easy-to-use abstraction for save/load of class data
* Eases access to various common game functions without needing to import the assembly themselves, removing the Mono / Il2Cpp dependency for your mod.
* Allow modders to live in the Managed environment, regardless of Mono / Il2Cpp development
* Lower the bearer of entry into S1 modding, especially for Il2Cpp builds
* Support mod developers by allowing them to compile a single assembly that works across both builds
* Who knows what else? We will have to see who all is willing to collaborate on this ❤️

## What Are the Limitations?
* S1API will NOT be the be-all and end-all. It's just not possible.
* Handle Il2Cpp / Mono communication when utilizing game assemblies as dependencies
* Cover all modding needs. It will never be as flexible as writing your own mod referencing game assemblies.

## How It's Designed to Work
S1API is designed to compile for Mono and Il2Cpp separately.
Mod users don't need to worry about this though. 
The standard installation ships with all builds and a plugin to dynamically load the proper version!

Mod developers can develop their mods on whichever build, Mono or Il2Cpp, without having to step into the Il2Cpp environment.
Caveat: If you do utilize Il2Cpp functionality within your mod, you lose cross compatibility.
I can't think of why you would. I wanted to make sure that is clarified though.

S1API is designed to be a tag-along as well.
If you want to do custom content specific to Mono or Il2Cpp, S1API can still assist with some of the common repetitive tasks.

## Want to Contribute?
This is a massive project with so many different areas to specialize in.
If you're interested in contributing, please do!
Look over the [CONTRIBUTING.md](CONTRIBUTING.md) for guidance on code standards and the process.
