# WebQL Application Webserver #

**Build a clientside WebApp and integrate easy Data from a MS-SQL Database**

-----
## Overview ##

1. webQL is an easy to install, asynchron Application-Server based on .NET, which leaves a small Footprint. 
1. webQL is fast, non blocking and supports more than 1000 Requests/Second.
1. webQL supports Routes, which comes from a Configuration File or can be defined by Code. If a Route matches an incoming Request, then an existing or self defined Modul will be executed. 
1. WebQL brings you seamless Access to an existing MS-SQL Database, without using the Complexity of the Entity Framework. The Result of your Query comes - agnostic, without defining Models - in JSON Format for simple clientside processing in Javascript. Arrangements are made to prevent SQL-Injection. Despite Webaccess you keep full control over your Database, by defining granular Items in the DBQuery-Configuration File or by Code.
1. webQL supports SSL-Connections. 

## Quickstart 
 
### 1) Edit Server-Configuration File

The Content of the WebQL.config.json is written in JSON Notation. For editing you can use any Editor or also a [JSON Online Editor](http://www.jsoneditoronline.org/). 

**Prefixes** defines a Port for listening. The Property `"Prefixes": ["http://+:8081/"` means your Server will listen on Port 8081 and you can request the Server by entering  `http://localhost:8081/` in your Browser.

If webQL doesn't run as Admin, then **UrlACLUser** gives you sufficient permission. This can be done by defining the property `"UrlACLUser": ""`. The empty string value means that you give permission to the current user, but you can set any other user like `"UrlACLUser": "system"` or `"UrlACLUser": "computer/user"`. After running webQL click the item 4) `Set Rights` on your Form and cancel the Certificate-Selector for this time. Now webQL set the Rights for the current user.  

**Rootpath** targets to your Webproject. You can set an absolute Path `C:\\root\webproject` or a relative path `root\webproject` which starts from your Application-Path. If your Application-Path is `C:\programs\webQL\bin` then your Project can be placed in `C:\programs\webQL\bin\root\webproject`   

**Minimal working WebQL.config.json File**
    
	{	
		"Prefixes": ["http://+:8081/"],		/* listen on Port 8081 -> http://localhost:8081/	*/
		"UrlACLUser": "",					/* "" means, take the current User and give him Rights to listen 	*/	
		"RootPath": "root\webproject",		/* relative Path from your Application-Path to your Webproject */
	}

### 2a) Edit Routes-Configuration File

In the Routes Configuration File, you can define Routes, which handles the incoming Request by matching the Request Rule with the UrlSegment. The Routes are handled in the Order listed in your Route-Configuration File. In the next Example the Request URL is '/css/app.css'. Now webQL first takes 'Route1' and here is no match from '/css/app.css' with '/*.js'. No webQL takes the next Route 'Route2' wich matches '/css/app.css' with '/+' and invokes the Method Action in Class with Attribute [ModulName("Module_NotFound")]. This Method responds the in `"FileName404"` declared File `"index.html"`.

	{
		"Route1":{
		    "Action":"Modul_File", /* [Modul_]File -> "Modul_File" or "File" */
		    "UrlSegment": "/*.js"  /*  Warning: Security Risk you can read any file in the Path of your Webproject */
    	},

		"Route2":{
		    "Action": "Modul_NotFound",
			"UrlSegment": "/+",	 		/* matches any path 'localhost:8081/dir1/dir2/file.css' */
			"FileName404":"404.html"  /* the File '/404.html' will in be responded */
    	},
	}


Allowed **Wildcards** for the **UrlSegment** are 

>     ? 			for single character
>      						
>     *             any characters to the next /*.js Match: /app.js/  noMatch: /library/controller.js 
>        
>     +             any characters to the end  /+.js Match: /app.js/  Match: /library/controller.js
>        
>     -Item         all but Item            /-DB/+   Match: /html/test.html   noMatch: /db/test.html
>     
>     (A|B|C)       A or B or C             /(JS|LIB|CSS|HTML)/*.(JS|CSS|HTML) Match: /lib/angular.min.js


Route: "Get HTML JS CSS Files"*

Reads any existing File from your Rootpath. We do a simple Demonstration, but in real Life give free Access to the Rootpath is a important Security Risk. For productive Work you have to make some Restrictions ie. `"UrlSegment": "/js/*.js"` allows only to read Files with the Extension JS in the directory JS  
		

**Minimal working Routes-Configuration File**

	{	
		"Responds static HTML-JS-CSS Files":  
		{
	      "Action":"Modul_File",
		  "UrlSegment": "/(lib|css|js|html)/*.(js|css|html)"  
	    },  
	    
		"AccessController":
		{
	      "Action":"Modul_Access",
		  "UrlSegment": "DB/+"      /*  Block Dir DB and all SubDir */
	    },

		"DBQuery Handler":
		{
			"Action": "Modul_DBQuery",		/*  Modul_DBQuery */
			"UrlSegment": "/DB/*"			/*  all request to /DB/ make a database query          */
		},
	
	    "File Not Found Use Index HTML":
		{
			"Action": "Modul_NotFound",
			"UrlSegment": "/*",	 
			"FileName404":"index.html"
	    }         
	}


-----
### 2b) Edit DBQuery-Configuration File

For controlling the Access from your Webclient to your MS-SQL Database you can define items in the DBQuery-Configuration. Here you can grant or restrict access as you need it. 

For Database Access the Client send a POST containing a DBQuery-Arguments Object 

    "Name":"Person","Filter":"lastname $starts mill", "Method":"R","Limit":256,"Sort":"lastname ASC, firstname ASC"


First webQL tries to match exactly the Value of incoming Name `{"Name":"Person",...}` with the Item `{"Items": {"Person": {...}, "NextItem":{...}, ... }}`. If there is no success, then webQL tries to match the wildcard items in the order inside your file. The incoming Name `{"Name":"Person",...}` matches with Item `{"Items": {"Per*": {...}, ... } }`.

The Item '*' matches all Tables and Views and therefore allows full Access to all Tables and Views in the Database described by your Connectionstring.   

    "*": { "AllowedMethods": "CRUD", "ConnectionString": " ...  " }

> WARNING: THIS IS A QUICK and DIRTY ACCESS TO YOUR MS-SQL DATABASE FOR DEMO ONLY
> IT COULD BE A SECURITY RISK TO ALLOW WILDCARD '*' ACCESS ALL TABLES OR VIEWS in DB 
> 
> IF INCOMING ARGUMENTS.NAME MATCHES THIS ITEM, DBQUERY TRIES TO RESOLVE THE ARGUMENTS.NAME as TABLE OR VIEW, IF THERE IS NO 'SQL':'SELECT * FROM TABLE' DEFINED 

In the next Example all Requests with a Name starting with pre `"Arguments":{"Name":"preAnyString"}` will mapped to `Select * FROM Table`

    "pre*": { "AllowedMethods": "CRUD", "SQL":"Select * FROM Table", "ConnectionString": " ...  " }

If the incoming POST Data contains `"Arguments":{"Name":"dboPerson.tblName"}` then the next Item matches and only **Read** Access to an existing Table or View with Schema 'dboPerson' and Tablename 'tblName'is granted

    "dboPerson.tblName": {"AllowedMethods": "R", "ConnectionString": " ... " }

In this Case all Requests `"Arguments":{"Name":"dboPerson.tblName"}` with a Schema starting `dbo` and TableName starting `tbl` will mapped only to an existing View with Schema 'dboPerson' and Tablename 'tblName'. You can restrict to Views, by setting `"AllowedTableType": "VIEW"`. Valid Values for **AllowedTableType** are 'TABLE', 'VIEW' or 'ALL'. If **AllowedTableType** is not set the default value is 'ALL'.

    "dbo*.tbl*": { "AllowedMethods": "CRUD", "AllowedTableType": "VIEW", "ConnectionString": " ...  " }

If the incoming POST Data contains `"Arguments":{"Name":"dboPerson.tblName"}` then the next Item matches and only **Read and Update** Access to the Table or View 'HiddenTable' is granted. In this case you can hide Tablenames from the Webclient.

    "dboPerson.tblName": {"AllowedMethods": "RU","SQL":"HiddenTable" ,"ConnectionString": " ... " }

If the incoming POST Data contains `"Arguments":{"Name":"dboPerson.tblName"}` then the next Item matches and only **Read and Update** Access to the SQL String is granted. In this case you can hide Tablenames from the Webclient.

    "dboPerson.tblName": {"AllowedMethods": "RU","SQL":"SELECT TOP 15 FROM HiddenTable" ,"ConnectionString": " ... " }


**Simple DBQuery-Configuration File**
 
	{	
		"Items": {
  
			/* ----------------------------------------------------------------------
				WARNING: THIS IS A QUICK and DIRTY ACCESS TO YOUR MS-SQL DATABASE FOR DEMO ONLY
				
				IT COULD BE A SECURITY RISK TO ALLOW WILDCARD '*' ACCESS ALL TABLES OR VIEWS in DB 

				IF INCOMING ARGUMENTS.NAME MATCHES THIS ITEM, DBQUERY TRIES TO RESOLVE THE  
				ARGUMENTS.NAME as TABLE OR VIEW, IF THERE IS NO 'SQL':'SELECT * FROM TABLE' DEFINED 
			   -----------------------------------------------------------------------*/
			
			"*": {    
					"AllowedMethods": "CRUD",		/* [C]reate,[R]ead,[U]pdate,[D]elete  */			
					"ConnectionString": "... your connectionstring ... ",
			}

		}  
	}

----
### 3) Explore Rootpath

Check your Definition of the Rootpath `'Rootpath':'root/webproject'`. Targeting to the right Directory, you can explore your Webproject. 

---
### 4) Set the Rights needed for Listening

UrlACL ....

----
### 5) Start or Stop your Server

A running Server shows a green Bar. A red Bar means your Server has stopped. After 10 Seconds running webQL the Server will try an **Autostart**. Any manual Interaction on the Form will prevent the Server from Autostarting, in this case you must explicit `Start` the Server.

**IMPORTANT:**

Dont forget to configurate the **Firewall** on your WebQL running Server to accept incoming Requests on the listening Ports.

For external Access from the Web you must allow **Port Forwarding** on your Router 
i.e. map Extern Port 80 -> to listening Intern Port 8081 on the IP-Adress of your Server running webQL.

---
### 6) Open your Project in the Browser

If the Server is running (displaying a green Bar) then you can start here any Browser with the first in Prefixes defined Port. 

    { "Prefixes": ["http://+:8081/"], ... }

This Prefix runs your Browser with **http://localhost:8081/**

---
### 7) Show Logfile

Here you can inspect your Logfile, if you have activated Logging to the File in the Server-Configuration with **'Logmode':'F'**. 

WebQL.config.json File with **Logmode**:

	{	
		/*  
			If 'Log' is omitted then default		->  'Log':'FQ',  
			Full Logging to 'F'ile and 'C'onsole	->  'Log':'FC',   
			No Logging, uses empty String			->  'Log':'',
			
			Options for Logging: 
			F	- 'F'ile		log to File
			C	- 'C'onsole		log to Console 
			Q	- re'Q'uest		log the Request
			S	- re'S'ponse	log the Response				
		*/
		"Logmode":"FQS",
		
		"Prefixes": ["http://+:8081/"],		/* listen on Port 8081 		*/
		"UrlACLUser": "",					/* take the current User  	*/	
		"RootPath": "root\webproject",		/* relative path from your application path to your webproject */	
	}


## Prerequisites ##

OS Windows 7 or newer

NET Framework 4.5

optional MS-SQL Database

## Configuration

### You need 4 Configuration Files in your Application Directory ####

All Configuration files uses the JSON - Notation. JSON or JavaScript Object Notation, is an open standard format that is originally derived from the JavaScript scripting language. It uses human-readable text to transmit data objects consisting of attribute–value pairs. It is used to transmit data between a web server and client. It works best with Javascript. If the Client makes a Data-Request to the server you get a JSON-Object as Data-Result, which could be handled perfektly by any Javascript-Framework like AngularJS. 

With webQL your Web Client (Javascript) sends a request in JSON Format to the webQL Server and your MS-SQL DB. You will get the result back to your Client in JSON Format, for further processing in Javascript. No Data Modeldefinition is necessary, 

- WebQL.config.json - configures the server

- WebQL.routes.json - defines all the Routes wich are in the order step by step checked if the server handles a request

- WebQL.mimeTypes.json - holds the information for the Mimetypes

- DBQuery.config.json - defines the Access to the MS-SQL Database. Here you add your Connectionstring to a MS-SQL DB. You can control the kind of Access to the Database. You can work with Tables, Views or predefined SQL-Strings. 


## Configuration of webQL Server with 'webQL.config.json' File ##


### Properties of the WebQL.config.json File ####

The Content of the WebQL.config.json is written in JSON Notation. For editing you can use any Editor or also a [JSON Online Editor](http://www.jsoneditoronline.org/).  
    
1) Use **Prefixes** to define which Uniform Resource Identifier (URI) webQL is listening. 

> "Prefixes": ["http://+:8081/", "https://*:4403/"] 

When a port is specified, the host element can be replaced with "\*" to indicate that the HttpListener accepts requests sent to the port if the requested URI does not match any other prefix. 
For example, to receive all requests sent to port 8080 when the requested URI is not handled by any HttpListener, the prefix is http://\*:8080/. Similarly, to specify that the HttpListener accepts all requests sent to a port, replace the host element with the "+" character, "https://+:8080". The "*" and "+" characters can be present in prefixes that include paths. [Get more Information from Microsoft, see under 'Remarks'](http://msdn.microsoft.com/en-us/library/vstudio/system.net.httplistener%28v=vs.110%29.aspx)

2) **UrlACLUser** adds an UrlACL for all prefixes with an user. [Get more Information from Microsoft](http://msdn.microsoft.com/en-us/library/ms733768%28v=vs.110%29.aspx)
 
Example: 
> "UrlACLUser": ""  => empty string use the current user 
> 
> "UrlACLUser": "system"  
> 
> "UrlACLUser": "everyone" 
> 
> "UrlACLUser": "computer/user"

3) Configure a Port with a SSL-Certificate using **SSLCertIpport, SSLCertHash and SSLCertAppId**

Here you can set your SSL Parameters. [Get more Information how to configure a Port with a SSL Certificate](http://msdn.microsoft.com/en-us/library/ms733791%28v=vs.110%29.aspx) 

---
Example 1
>l "SSLCertIpport": "192.168.1.1:443",
> 
> ["SSLCertHash": "",] 
> 
> ["SSLCertAppId": ""] 

=> webQL executes

netsh http add sslcert ipport=192.168.1.1:443 certhash=*'shows a Certificate Selector'* appid=*'use App-ID of running webQL'*

----

Example 2
> "SSLCertIpport": "0.0.0.0:4403"
> "SSLCertHash": "0000000000373ed9cd0c315bxc6dc1c08da5e6" 
> "SSLCertAppId": "{00112233-4455-6677-8899-AABBCCDDEEFF}" 

=> webQL executes

netsh http add sslcert ipport=0.0.0.0:4403 certhash=0000000000373ed9cd0c315bxc6dc1c08da5e6 appid={00112233-4455-6677-8899-AABBCCDDEEFF}

### Minimalistic WebQL.config.json File ####

		{
			"Prefixes": ["http://+:8081/"],		

			"UrlACLUser": "",	
		
			"SSLCertIpport": "0.0.0.0:4403"
			"SSLCertHash": "" 
			"SSLCertAppId": "" 
	
			"RootPath": "webproject"			
		}
    	
### WebQL.config.json File 


    {	
    
    	/*  
			If 'LogMode' is omitted then default		->  'LogMode':'FQ',  
    		Full Logging to 'F'ile and 'C'onsole	->  'LogMode':'FC',   
    		No Logging, uses empty String			->  'LogMode':'',
    		
    		Options for Logging: 
    		F	- 'F'ile		log to File into directory 'Log' in your bin path
    		C	- 'C'onsole		log to Console 
    		Q	- re'Q'uest		log the incoming Request 
    		S	- re'S'ponse	log the outgoing Response    			
    	*/
    	"LogMode":"FSQ", 
    	

    	/* 
			Define Ports for listening, 	
    		Details you will find in the decription of the System.Net.HttpListener Class
    	*/
    	"Prefixes": [
    	"http://+:8081/",
    	"https://*:4403/"
    	],
    

    	/* 
    		Set Rights for SSL and URLACL 
    	*/
    	"SSLCertIpport": "192.168.0.137:4403",
    	"SSLCertHash": "",
    	"SSLCertAppId": "",		/* 	if empty take the AppID of WebQl Application */    	

		"UrlACLUser": "",		/* 
									reserve an URL for all in Prefixes defined ports. 
								   	Parameter is a User 'myComputer/User' or 'system' or if empty take the current User  
								*/
    	

    	/* 
			absolute or relative PATH for Webserver Project allowed 
    	   	the relative Path points to the bin path  
    	*/		
    	"RootPath": "C:/mypath/to/root/myWebproject"			

    }


----------

### WebQL.routes.json ###
In this File you can define in JSON Notation any Route, which should be handled, by WebQL. 

All Routes will be checked in the order appearing in the 'WebQL.routes.json' File, until your Request URI matches a Route. 

If there is **no matching Route**, webQL brings up a Default 404-Handler. 

If there is **a matching Route**, then the containing Property "Action":**"MyModulName"** targets to the class with the attribute [ModulName(**"MyModulName"**)] and invokes the method Action in this class.
		 

> "Action":**"MyModulName"** calls the async Method 'Action()' in the Class with Attribute [ModulName(**"MyModulName"**)] 

#### WebQL.routes.json with only one Route 'MyFirstRoute'  
    {
		"MyFirstRoute":   /* use a unique Name for the Route*/
	    {
	    	
			/*  
				defines the route, which will be matched against the URL of the incoming Request 
	    	
	    		accepts all requests in the form 
	    		http://HOST/html/foo.html 
	    		http://HOST/html/foo.js 
	    		http://HOST/html/foo 
	    		http://HOST/html/dir/foo.txt 
	    	*/
	    	"UrlSegment": "/html/*",   
		

	    	/* 
	    		If Route matches the incoming URI then the "Action":"MyModulName" 			
				calls a class with the attribute [ModulName("MyModulName")]   				 
			*/	
	    	"Action":"MyModulName", 
		

	    	/*
	    		You can use here any further Properties like 'AnyParameterName', 
	    		this Properties can be used in your corresponding Modul 'MyModulName'
				->  string p= ResponseInfo.CurrentRoute["AnyParameterName"];
	    	*/
			"AnyParameterName":"Value of any parameter",
	    	"AnotherParameterName":"Value of another parameter"
	    	
	    }
	}

-----

#### "Action":"MyModulName" needs a corresponding Class with Attribute [ModulName("MyModulName")]
		[ModulName("MyModulName")]
    	public class MyAnyNameClass : IModul
    	{
		...
			public async Task<ModulResult> Action(){
				string parameter = ResponseInfo.CurrentRoute["AnyParameterName"];
				string parameter = ResponseInfo.CurrentRoute["AnotherParameterName"];
				...
				return ModulResult.Response; 
			}
        ...
		}

#### Customise your Response programmatically using Modules ####

A correct Modul is a Class which implements the Interface 'IModul' and has the Attribute 'ModulName'. The Constructor will be called with an instance of the Responder Class, which will be created for every incoming Request. This ResponseInfo holds the Information to build up your Response. The async Method **Action**  will be called and returns a ModulResult Value. If you want to finish the Request, you can respond by returning the Enum Value **ModulResult.Response**    
	
        
        [ModulName("My_Modul_Name")]
        public class Demo : IModul
        {

            private Responder ResponseInfo;
            public Demo(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }

            public async Task<ModulResult> Action()
            {
				
				// ... simple write your response with the Helperfunction of Responder here
				ResponseInfo.setResponseComplete("txt","Hello world",HttpStatusCode.OK);
				
				// ... finish to work up the Loop of Routes and send the Response...
                return ModulResult.Response;

            }
        }


		public enum ModulResult
        {

            ReStart,     // Restart Route loop 
            Response,    // break loop of routes and send response
            Continue,    // simply go on...
        }
	
###Sample of WebQL.routes.json:	###
	
	{
	
		/* ------ "#ROUTE" -the Hashtag marks the default values route ------- */
		"#Default":
		{
				  "Action":"Modul_File",
		}, 
	
		/* ------ "-ROUTE" -the minus marks the route as inaktiv --------- */
	
		"-Modul_SetHeader":
		{ 	/* first in routes */		 
				"Action": "Modul_SetHeader",
				"Header1": "Application-ID|wotan@works (c)2014",     
				"Header2": "Ganyweb|2.0 Beta",
		},	
		
		"HTML":
		{
	       		"Action":"Modul_File",
				"UrlSegment": "/html/*.html"
		},
	    	
		"JS":
		{
		  		"Method":"GET",
		  		"Action":"Modul_File",
		  		"UrlSegment": "/js/*.js"
		},
	
		/* ---------- If you come here NO ROUTE HAS MATCHED and respond the not found file---------------- */    
		"NotFound":
		{
				"Action": "Modul_NotFound",
				"UrlSegment": "/*",
				"FileName404":"404.html" 		
		}
		      
	}


----------

### WebQL.mimeTypes.json ###

Definition of all MIME types in JSON Format. [Here](http://www.iana.org/assignments/media-types/media-types.xhtml) you can find an CSV-File with all Formats. 

#### Sample of WebQL.mimeTypes.json ####
	{
	
		"css" : "text/css",
		"htm" : "text/html",
		"html" : "text/html",
		"jpeg" : "image/jpeg",
		"jpg" : "image/jpeg",
		"json" : "application/json",        
		"js" : "application/javascript",
		"txt" : "text/plain",
		/* ... */
	
	}

----------

### DBQuery.config.json ###

Here you control the access to your MS-SQL Database, by adding items in JSON Notation.     

#### DBQuery.config.json File with Access to Table 'Person' and SQL-String  ####

		{
			"Items": 
				{		  			
					"Person": {    
						"AllowedMethods": "CRUD",		/* [C]reate,[R]ead,[U]pdate,[D]elete */
						"SQL":"Person" 				/* ["SQL":"Table|VIEW|SQL-STRING"]  	
					},

					"YoungPerson": {    
						"AllowedMethods": "R",			/* Read only */
						"SQL":"Select * FROM Person WHERE age <30 " 			
					}		
				}  
		}

#### DBQuery.config.json File with Wildcard Access ####

		{
			"Items": 
				{
		  
					/* ----------------------------------------------------------------------
						THIS IS A QUICK and DIRTY ACCESS TO YOUR MS-SQL DATABASE, BUT IT IS 
						A SECURITY RISK TO ALLOW WILDCARD '*' ACCESS TO ALL TABLES OR VIEWS IN YOUR DATABASE 
					   ----------------------------------------------------------------------- */
					
					"*": {    
						"AllowedMethods": "CRUD",		/* [C]reate,[R]ead,[U]pdate,[D]elete
						"AllowedTableType": "ALL",		/* TABLE, VIEW, ALL = [Empty]  only in ITEMS for Matching */			
						"ConnectionString": "... your connectionstring..."
					}
		
				}  
		}

### DBQuery.config.json File with 'Settings' ###

In Settings you can define global Parameters like the 'ConnectionString', 'DefaultInsertValues', 'DefaultColumnFormat'. Restrict Access by defining a 'Token' which is needed to be sent with every Request.  

All these Settings and Items can also be programmatically overwritten. For Example:
> DbQuery.Settings.DefaultInsertValues.ForDateTime=DateTime.Now;     


	{
		"Settings": 
		{
			
			"ConnectionString": "data source=MySQLServer;database=myDatabase;user id=userFoo;password=;",		
			
			"Token": null,
			
			"DefaultColumnFormat": {
				"ForIntegers": null,
				"ForDecimals": null,
				"ForDateTime": "d.M.yyyy",
				"ForTime": null,
				"ForBooleanTrue": true,
				"ForBooleanFalse": false
			},
			
			"DefaultInsertValues": {
				"ForByte": 0,
				"ForInt16": 0,
				"ForInt32": 0,
				"ForInt64": 0,
				"ForDecimal": 0.0,
				"ForDouble": 0.0,
				"ForSingle": 0.0,
				
				"ForDateTime": "2000-01-01T00:00:00",   
				/* 	can be programmatically overwritten -> 	DbQuery.Settings.DefaultInsertValues.ForDateTime=DateTime.Now; 
				*/
				
				"ForTime": "00:00:00",
				"ForBoolean": false,
				"ForString": ""
			}
		},
	    

		"Items": 
		{
	  
			"myLookup": {
			"AllowedMethods": "CRUD",
			/* you can override the general ConnectionString  */
			"ConnectionString": "data source=runner;database=dothealth-K667;user id=sa;password=;",
			"Sql": "bettenspiegel"    
			},
	
			"bettenspiegelSql": {
			"AllowedMethods": "CRUD",
			"ConnectionString": "data source=runner;database=dothealth-K667;user id=sa;password=;",
			"Sql": "select (round(avg(total/bocche), 2)), trade from catasto where series = 1 and total <> 0 and gender = 1 group by trade"
	
			},
	
			"Sample1": {
				"AllowedMethods": "CRUD",
				"ConnectionString": null,
				"CommandTimeOut": 0,
				"Sql": "SELECT Befund.Nr as bnr, Befund_Details.Nr as bdnr, Kategorien.Nr as knr, Befund.AnlegeDat, Befund.Pat_Nr, Befund_Details.Inhalt, Befund_Details.Inhalt_Text, Kategorien.Titel, \r\n                    Kategorien.wk_autoinsert, Kategorien.Code, Befund.AnName, Befund.AnVorname, Befund.AnStrasse, Befund.Uebermittlung, \r\n                    Befund.wk_Diktat, Befund.wk_Schreibkraft, Befund.wk_Unterschrift, Befund.wk_UnterschriftDatum, Befund.wk_AusgangsDatum, \r\n                    Befund.wk_Status, Befund.wk_AusgangsUnterschrift, Befund.wk_AusgangsArt, Kategorien.Position\r\n                    FROM    Befund INNER JOIN\r\n                    Befund_Details ON Befund.Nr = Befund_Details.Befundnummer INNER JOIN\r\n                    Kategorien ON Befund_Details.Kategorie = Kategorien.Nr",
				"CultureName": "de-AT",
				"Token": null,
				"ColumnFormat": {},
				"DefaultInsertArgs": {
					  "[LastLoginDate]": {
						"Value": "getdate()",
						"IsExpression": true
					  }
				}
			},
	
			/* ----------------------------------------------------------------------
				THIS '*' IS A SCURITY RISK TO ALLOW ALL METHODS 'CRUD' 
				THIS '*' IS A SCURITY RISK TO ALLOW WILDCARD FOR ALL TABLES OR VIEWS in DB 
			   -----------------------------------------------------------------------*/
			   
			"*": {    
				"AllowedMethods": "CRUD",
				"ConnectionString": null
			},
	
	
			"DC": {
				"AllowedMethods": "CRUD",
				"ConnectionString": null,
				"CommandTimeOut": 0,
				"Sql": "SELECT top 100 percent count(text) c, max(dat)  d, text, max(perm_)  p FROM dia group by text order by p desc , d desc, c desc",
				"CultureName": "de-AT",
				"Token": null,
				"ColumnFormat": {},
				"DefaultInsertArgs": {
					"[LastLoginDate]": {
					"Value": "getdate()",
					"IsExpression": true
					}
				}
			},
			"K": {
				"AllowedMethods": "CRUD",
				"ConnectionString": null,
				"CommandTimeOut": 0,
				"Sql": "SELECT    ID, Pat_nr, Dat, Kurz, Text, Art, Lang, 'K' Tablename FROM dbo.kart \r\n                    UNION ALL \r\n                    SELECT    ID, pat_nr, dat, kurz, text, CAST(perm_ AS char) , '' , 'D' FROM dbo.diagnose \r\n                    UNION ALL \r\n                    SELECT    ID, pat_nr, dat, datei, text, CAST(gelesen AS CHAR) , langtext, 'F' FROM dbo.fremdbefunde \r\n                    UNION ALL \r\n                    SELECT    Befund_Details.Nr, pat_nr, Befund.AnlegeDat, Kategorien.Titel, CAST(Befund.Nr AS CHAR), CAST(Befund.wk_Status AS CHAR), Befund_Details.Inhalt_Text, 'B' AS Tablename \r\n                    FROM      Befund INNER JOIN Befund_Details ON Befund.Nr = Befund_Details.Befundnummer INNER JOIN Kategorien ON Befund_Details.Kategorie = Kategorien.Nr \r\n                    UNION ALL \r\n                    SELECT    ID, pat_nr, dat, ds, text, '0' , CAST(mag AS char), 'R' FROM dbo.pmedikam \r\n                    UNION ALL \r\n                    SELECT    ID, pat_nr, dat, kk_kurz, std_code, rgruppe, lgruppe, 'P' FROM dbo.leistung ",
				"CultureName": "de-AT",
				"Token": null,
				"ColumnFormat": {},
				"DefaultInsertArgs": {
					"[LastLoginDate]": {
					"Value": "getdate()",
					"IsExpression": true
					}
				}
			},
			"KD": {
				"AllowedMethods": "CRUD",
				"ConnectionString": null,
				"CommandTimeOut": 0,
				"Sql": "SELECT    ID, Pat_nr, Dat, Kurz, Text, Art, Lang, 'K' Tablename FROM dbo.kartei \r\n                     
				UNION ALL \r\n  
				SELECT    ID, pat_nr, dat, kurz, text, CAST(perm_ AS char) , '' , 'D' FROM dbo.diagnose ",
				"CultureName": "de-AT",
				"Token": null,
				"ColumnFormat": {},
				"DefaultInsertArgs": {
					"[LastLoginDate]": {
					"Value": "getdate()",
					"IsExpression": true
					}
				}
			},
			"P": {
				"AllowedMethods": "CRUD",
				"ConnectionString": null,
				"CommandTimeOut": 0,
				"Sql": "SELECT wartelis.wl as warteliste, kas.rgb as rgb, kas.abr_int as abr_int, LTRIM(RTRIM(patient.titel)) + ' ' + LTRIM(RTRIM(patient.famname)) + ' ' + LTRIM(RTRIM(patient.vorname)) as name, patient.*  FROM  dbo.patient INNER JOIN dbo.kasse ON dbo.patient.kk_kurz = dbo.kasse.kk_kurz LEFT OUTER JOIN dbo.wartelis ON dbo.patient.pat_nr = dbo.wartelis.pat_nr",	
				"CultureName": "de-AT",
				"Token": null,
				"ColumnFormat": {},
				"DefaultInsertArgs": {
					"[LastLoginDate]": {
					"Value": "getdate()",
					"IsExpression": true
					}
				}
			}
	
	
	
		}
	  
	}


### Arguments and Result

#### Arguments

	Methode die durchgeführt werden soll C,R,U,D
    public string Method { get; set; }
    Name des Schemas
    public string Schema { get; set; }
    Name der Tabelle oder Sicht
    public string Table { get; set; }
    Get-Anweisung bei Read
    public string Get { get; set; }
    Filter-Anweisung bei Read, Update oder Delete
    public string Filter { get; set; }
    Maximale Anzahl der abgefragten Datensätze bei Read (keine Einschränkung, wenn kleiner 0 oder leer)
    public string Limit { get; set; }
    Sort-Anweisung bei Read
    public string Sort { get; set; }
    Set-Anweisung bei Create oder Update
    public string Set { get; set; }
    Message wird durchgereicht
    public string Message { get; set; }
    Index des ersten zurückgegebenen Datensatzes bei Read (0, wenn kleiner 0 oder leer)
    public string PageIndex { get; set; }
    Maximale Anzahl der zurückgegebenen Datensätze bei Read (keine Einschränkung, wenn kleiner 1 oder leer)
    public string PageSize { get; set; }

Example

	var args={
		Method:"CRUD",	
		Name:"ItemInDBQuery",	
		Filter:"Age $lt 30",	
		Sort:"Prename"
	}

	$http.post("/db/person",args)
		.success(function(result){

			// result.data contains the Data from your DB as array of JSON-Objects 
			// result.model contains the Data Model from your DB		

		})

 

#### RESULT 
 
    Methode die verwendet wurde C,R,U,D
    public string Method { get; set; }
    Name des Schemas
    public string Schema { get; set; }
    Name der Tabelle oder Sicht
    public string Table { get; set; }
    Get-Anweisung bei Read
    public string Get { get; set; }
    Filter-Anweisung bei Read, Update oder Delete
    public string Filter { get; set; }
    Maximale Anzahl der abgefragten Datensätze bei Read (keine Einschränkung, wenn kleiner 0 oder leer)
    public string Limit { get; set; }
    Sort-Anweisung bei Read
    public string Sort { get; set; }
    Set-Anweisung bei Create oder Update
    public string Set { get; set; }
    Message wird durchgereicht
    public string Message { get; set; }
    Index des ersten zurückgegebenen Datensatzes bei Read (0, wenn kleiner 0 oder leer)
    public string PageIndex { get; set; }
    Maximale Anzahl der zurückgegebenen Datensätze bei Read (keine Einschränkung, wenn kleiner 1 oder leer)
    public string PageSize { get; set; }
    
    Die abgefragten Spaltennamen bei Read
    public string Model { get; set; }
    Die zurückgegebenen Datensätze bei Read
    public string Data { get; set; }
    Anzahl der zurückgegebenen(Read) oder betroffenen(Create,Update,Delete) Datensätze
    public string Affected { get; set; }
    Gesamtanzahl der durchlaufenen Datensätze bei Read
    public string TotalReads { get; set; }
    Fehlermeldung bei Ausnahme
    public string Error { get; set; }