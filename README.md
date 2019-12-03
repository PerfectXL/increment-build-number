# increment-build-number

*Utility script to update the projects' version numbers for a solution*

### Help page

```
Usage: increment-build-number [PATH] [--major | --minor] [--build]

  PATH must point to a git repository containing a Visual Studio solution.

  Options:
    --major    Increment the major version number, set minor version number to
               zero.
    --minor    Increment the minor version number.
    --build    Increment the build number (default).

  If you specify --major or --minor and omit --build, the build number will be
  reset to zero.
  If you use --major or --minor in combination with --build, the build number
  will be incremented as well.

  The revision number is not used and will always be zero.
```

### Usage in the various projects

- In all reposistories, to increment only the build number, run `increment-build-number` without options. (Or simply double-click the executable.)

- In [PerfectXL-Auditor](https://github.com/PerfectXL/PerfectXL-Auditor) the build number is never reset to zero. The command for incrementing the minor version is: `increment-build-number --minor --build`

- In [PerfectXL-Compare](https://github.com/PerfectXL/PerfectXL-Compare) the build number is set to zero when the minor version number is incremented. The command for this is: `increment-build-number --minor`

- To increment the major version number, use `--major` instead of `--minor` in the commands above.

### Building the executable `increment-build-number.exe`

The projects depends on [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/). If you build for release, this assembly is merged into the executable, so the whole exectuable/script consists of just one file.

Target machines still need to have the [.NET Framework 4.6.2](https://www.microsoft.com/en-us/download/details.aspx?id=53344) installed.
