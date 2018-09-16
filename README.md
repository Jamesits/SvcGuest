# SvcGuest

Register any executable as a Windows service with a (partially) systemd-compatible unit config.

If you do not want to build it yourself, binary releases are in the [releases](https://github.com/Jamesits/SvcGuest/releases) page.

## Usage

You need a `your-program.service` config file first. An example can be found in the `examples` directory.

* `svcguest.exe --install --config your-program.service` to register the service
* `svcguest.exe --uninstall --config your-program.service` to remove the service
* `svcguest.exe --help` for a complete help

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
    * [ ] RemainAfterExit
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
