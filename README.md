# SvcGuest

Register any executable as a Windows service with a (partially) systemd-compatible unit config. What SvcGuest can achieve is a lot like [NSSM (the Non-Sucking Service Manager)](https://nssm.cc) but SvcGuest is designed to be shipped along with a larger piece of software rather than be used by end user directly.

[![Build status](https://dev.azure.com/nekomimiswitch/General/_apis/build/status/SvcGuest)](https://dev.azure.com/nekomimiswitch/General/_build/latest?definitionId=2)

Binary releases are in the [releases](https://github.com/Jamesits/SvcGuest/releases) for your convenience. 

## Usage

### Software Distributor

If you wrote a piece of software and doesn't want to adapt to the Windows Service interfaces yourself, you can distribute a binary release of `SvcGuest.exe` and your `program.service` config file ([example](examples/)) with your software, and run `svcguest.exe --install --config program.service` (remember to elevate!) during your software installation.

### End User

Assume you have a program that you want to run at startup (before user login) but don't want to use legacy dirty methods like Task Scheduler. You need to put a `SvcGuest.exe`, a `your-program.service` config file (an [example](examples/) is available) into the same directory as the program (recommended, other directories are OK too).

* Right click on `svcguest.exe` -> "Run as administrator" to install all units interactively
* `svcguest.exe --install --config your-program.service` to register the service
* `svcguest.exe --uninstall --config your-program.service` to remove the service
* `svcguest.exe --help` for a complete help

[MPL 2.0 License](LICENSE) allows distributing `SvcGuest.exe` with your software for free with some limitations. Ask your lawyer for advice if in doubt.

## Features

Features supported on the master branch (for the releases, see the `.service` file provided):

* [x] Unit
    * [x] Description
    * [x] Documentation
* [x] Service
    * [x] Type
        * [x] simple
        * [ ] forking
        * [ ] oneshot
        * [ ] idle
    * [x] User
    * [x] RemainAfterExit
    * [x] ExecStartPre
    * [x] ExecStart
    * [x] ExecStartPost
    * [x] ExecStop
    * [x] ExecStopPost
    * [ ] Environment
    * [ ] EnvironmentFile
    * [ ] PassEnvironment
    * [ ] UnsetEnvironment
    * [x] WorkingDirectory
    * [ ] CPUAffinity
* [ ] Install
    * [ ] WantedBy
    * [ ] RequiredBy

## Notes

Master branch is unstable; please use a release tag. It is recommended to upgrade to the latest revision; but if you have no issue running one of the releases and don't need any new function, then you don't need to upgrade to a later minor version.

If this project is helpful to you, please consider buying me a coffee.

[![Buy Me A Coffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/Jamesits) or [PayPal](https://paypal.me/Jamesits)
