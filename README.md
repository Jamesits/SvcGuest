# SvcGuest

Register any executable as a Windows service with a (partially) systemd-compatible unit config.

## Usage

You need a `your-program.service` config file first. An example can be found in the `examples` directory.

* `svcguest.exe --install --config your-program.service` to register the service
* `svcguest.exe --uninstall --config your-program.service` to remove the service
* `svcguest.exe --help` for a complete help

## Features

* [x] Unit
    * [x] Description
    * [x] Documentation
* [x] Service
    * [x] Type
        * [x] simple
        * [ ] forking
        * [ ] oneshot
        * [ ] idle
    * [ ] RemainAfterExit
    * [ ] ExecStartPre
    * [x] ExecStart
    * [ ] ExecStartPost
    * [ ] ExecStop
    * [ ] ExecStopPost
    * [ ] Environment
    * [ ] EnvironmentFile
* [ ] Install
    * [ ] WantedBy
    * [ ] RequiredBy

## Notes

If you consider this helpful, please consider buying me a coffee.

[![Buy Me A Coffee](https://www.buymeacoffee.com/assets/img/custom_images/black_img.png)](https://www.buymeacoffee.com/Jamesits) or [PayPal](https://paypal.me/Jamesits)
