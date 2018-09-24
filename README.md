# SvcGuest

Register any executable as a Windows service with a (partially) systemd-compatible unit config.

[![Build status](https://dev.azure.com/nekomimiswitch/General/_apis/build/status/SvcGuest)](https://dev.azure.com/nekomimiswitch/General/_build/latest?definitionId=2)

If you do not want to build it yourself, binary releases are in the [releases](https://github.com/Jamesits/SvcGuest/releases) page.

## Usage

### End User

Assume you have a program that you want to run at startup (before user login) but don't want to use legacy dirty methods like Task Scheduler. You need to put a `SvcGuest.exe`, a `your-program.service` config file (an example can be found in the `examples` directory) into the same directory as the program (recommended, other directories are OK too).

* Right click on `svcguest.exe` -> "Run as administrator" to install all units interactively
* `svcguest.exe --install --config your-program.service` to register the service
* `svcguest.exe --uninstall --config your-program.service` to remove the service
* `svcguest.exe --help` for a complete help

### Software Distributor

If you wrote a piece of software and doesn't want to adapt to the Windows Service interfaces yourself (since supervisors like `systemd` and `launchd` are widely available on other OSes, and it is sometimes tricky to implement these in some languages), you can distribute a binary release of `SvcGuest.exe` and your `program.service` config file with your software, and run `svcguest.exe --install --config program.service` during installation.

## Features

Features supported on the master branch (not the releases):

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
    * [ ] ExecStartPre
    * [x] ExecStart
    * [ ] ExecStartPost
    * [ ] ExecStop
    * [ ] ExecStopPost
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

If you consider this helpful, please consider buying me a coffee.

[![Buy Me A Coffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/Jamesits) or [PayPal](https://paypal.me/Jamesits)
