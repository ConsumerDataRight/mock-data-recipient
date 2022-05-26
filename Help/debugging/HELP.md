<h2>To get started, clone the source code</h2>
<div style="margin-left:18px;">
1. Create a folder called CDR<br />
2. Navigate to this folder<br />
3. Clone the repo as a subfolder of this folder using the following command;<br />
<div style="margin-left:18px;">
git clone https://github.com/ConsumerDataRight/mock-data-recipient.git<br />
</div>
4. Install the required certificates. See certificate details <a href="../../CertificateManagement/README.md" title="Certificate Management" alt="Certificate Management - CertificateManagement/README.md"> here</a>.<br />
5. Start the projects in the solution, can be done in multiple ways, examples below are from .Net command line and using MS Visual Studio<br />
</div>

<h2>.Net command line</h2>
<div style="margin-left:18px;">
<p>1. Download and install the free <a href="https://docs.microsoft.com/en-us/windows/terminal/get-started" title="Download the free Windows Terminal here" alt="Download the free MS Windows Terminal here">MS Windows Terminal</a>
<br />
2. Use the <a href="../../Source/Start-Data-Recipient.bat" title="Use the Start-Data-Recipient .Net CLI batch file here" alt="Use the Start-Data-Recipient .Net CLI batch file here">Start-Data-Recipient</a> batch file to build and run the required projects to start the Mock Data Recipient,
<br />
this will create the LocalDB instance by default and seed the database with the supplied sample data.
</p>

[<img src="./images/DotNet-CLI-Running.png" height='180' width='800' alt="Start projects from .Net CLI"/>](./images/DotNet-CLI-Running.png)

<p>LocalDB is installed as part of MS Visual Studio if using MS VSCode then adding the MS SQL extension includes the LocalDB Instance.</p>
<p>You can connect to the database from MS Visual Studio using the SQL Explorer, or from MS SQL Server Management Studio (SSMS) using
	the following settings; <br />
	Server type: Database Engine <br />
	Server name: (LocalDB)\MSSQLLocalDB <br />
	Authentication: Windows Authentication<br />
</p>
</div>

<h2>MS Visual Studio</h2>
<div style="margin-left:18px;">
<p>To launch the application using MS Visual Studio,</p>

<p>1. Select the project to start.</p>

[<img src="./images/MS-Visual-Studio-Start.png" height='300' width='600' alt="Start the projects"/>](./images/MS-Visual-Studio-Start.png)

<p>2. Then start the project (Ctrl + F5 or F5 or Debug > Start Debugging).</p>

[<img src="./images/MS-Visual-Studio-Running.png" height='300' width='600' alt="Projects running"/>](./images/MS-Visual-Studio-Running.png)

An output window will be launched for the selected project started.<br />
This will show the logging messages as sent to the console of the running project.
<br />

<p><h3>Debugging the running project using MS Visual Studio can be performed as follows;</h3>

<p>1. Select the project you want to debug and place the appropriate breakpoints as desired.</p>

[<img src="./images/Debug-using-MS-Visual-Studio-pt1.png" height='300' width='600' alt="Place breakpoint(s) in the projects"/>](./images/Debug-using-MS-Visual-Studio-pt1.png)

<p>2. Start a new debug instance for the selected project (F5 or Debug > Start Debugging).</p>
<div style="margin-left:18px;margin-top:-12px;">
	A new output window for the debug project will be started.
</div>
<br />

[<img src="./images/Debug-using-MS-Visual-Studio-pt2.png" height='300' width='600' alt="Start a new debug instance"/>](./images/Debug-using-MS-Visual-Studio-pt2.png)

<p>The browser window will be started with the Mock Data Recipient solution.</p>

[<img src="./images/Launch-application-in-browser.png" height='300' width='600' alt="Newly started output window"/>](./images/Launch-application-in-browser.png)

</div>