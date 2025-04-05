# How to contribute to SFMail

## Report a bug

- Please check [issues](https://github.com/DougHennig/SFMail/issues) if the bug has already been reported.

- If you're unable to find an open issue addressing the problem, open a new one. Be sure to include a title and clear description, as much relevant information as possible, and a code sample or an executable test case demonstrating the expected behavior that is not occurring.

## Fix a bug or add an enhancement

- Fork the project: see this [guide](https://www.dataschool.io/how-to-contribute-on-github/) for setting up and using a fork.

- Make whatever changes are necessary.

- If you make changes to SFMail.prg, change the cVersion property to reflect the current date, such as "2025.04.06" for April 6, 2025.

- If you make changes to the source for SMTPLibrary2.dll, change the assembly version number to reflect the current date, such as "2025.04.06" for April 6, 2025.

- Edit the Version setting in *BuildProcess\ProjectSettings.txt* to be the same as the cVersion property in SFMail.prg.

- If you added or removed files, update *BuildProcess\InstalledFiles.txt* as necessary.

- Describe the changes at the top of *ChangeLog.md*.

- If you haven't already done so, install VFPX Deployment: choose Check for Updates from the Thor menu, turn on the checkbox for VFPX Deployment, and click Install.

- Start VFP and CD to the project folder.

- Run the VFPX Deployment tool to create the installation files: choose VFPX Project Deployment from the Thor Tools, Application menu. Alternately, execute ```EXECSCRIPT(_screen.cThorDispatcher, 'Thor_Tool_DeployVFPXProject')```.

- Commit the changes.

- Push to your fork.

- Create a pull request; ensure the description clearly describes the problem and solution or the enhancement.

----
Last changed: 2023-01-22