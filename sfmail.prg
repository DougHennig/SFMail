* Test SFMail using SMTP. Change the settings to the correct values for your
* mail server.

local loMail, ;
	llReturn
loMail = createobject('SFMail')
with loMail
	.cServer      = 'MyMailServer.com'
	.cUser        = 'MyUserName'
	.cPassword    = 'MyPassword'
	.nSMTPPort    = 25
	.cSenderEmail = .cUser
	.cSenderName  = 'My Name'
	.cRecipients  = 'someone@somewhere.com'
	.cSubject     = 'Test email'
	.cBody        = 'This is a test message. ' + ;
		'<strong>This is bold text</strong>. ' + ;
		'<font color="red">This is red text</font>'
	.cAttachments = getfile('jpg')
	llReturn      = .SendMail()
	if llReturn
		messagebox('Message sent')
	else
		messagebox('Message not sent: ' + .cErrorMessage)
	endif llReturn
endwith

*==============================================================================
* Class:			SFMail
* Purpose:			Wrapper to send email via MAPI or SMTP
* Author:			Doug Hennig
* Last revision:	03/22/2021
*==============================================================================

define class SFMail as Custom
	cAttachments     = ''		&& A comma-separated list of attachments for the email
	cBCCRecipients   = ''		&& A comma- or semicolon-separated list of BCC recipients for the email
	cBody            = ''		&& The email body
	cCCRecipients    = ''		&& A comma- or semicolon-separated list of CC recipients for the email
	cErrorMessage    = ''		&& The text of any error
	cLogFile         = ''		&& The name of a log file (SMTP only)
	cPassword        = ''		&& The password for the mail server (SMTP only)
	cRecipients      = ''		&& A comma- or semicolon-separated list of recipients for the email
	cReplyTo         = ''		&& The Reply To address for the email (SMTP only)
	cSenderEmail     = ''		&& The email address of the sender (SMTP only)
	cSenderName      = ''		&& The name of the sender (SMTP only)
	cServer          = ''		&& The mail server address (SMTP only)
	cSubject         = ''		&& The email subject
	cUser            = ''		&& The user name for the mail server (SMTP only)
	lUseMAPI         = .F.		&& .T. to use MAPI or .F. to use SMTP
	nSecurityOptions = 1		&& The SecureSocketOptions value to use (SMTP only)
	nSMTPPort        = 25		&& The SMTP port to use (SMTP only)
	nTimeout         = 30		&& The email timeout in seconds

*==============================================================================
* Method:			SendMail
* Purpose:			Sends the email
* Author:			Doug Hennig
* Last revision:	03/22/2021
* Parameters:		none
* Returns:			.T. if the message was sent
* Environment in:	the properties are set for emailing
* Environment out:	none
*==============================================================================
		
	procedure SendMail
		local llReturn
		This.cErrorMessage = ''
		if This.lUseMAPI
			llReturn = This.SendMailMAPI()
		else
			llReturn = This.SendMailSMTP()
		endif This.lUseMAPI
		return llReturn
	endproc

*==============================================================================
* Method:			SendMailMAPI
* Purpose:			Sends the email using MAPI
* Author:			Doug Hennig
* Last revision:	03/16/2020
* Parameters:		none
* Returns:			.T. if the message was sent
* Environment in:	the properties are set for emailing
*					VFPExMAPI.fll is loaded or available to be loaded
* Environment out:	VFPExMAPI is added to SET LIBRARY if it wasn't loaded
*					before
*					the message may have been sent
*==============================================================================
		
	protected procedure SendMailMAPI
		#define MAPI_TO				1
		#define MAPI_CC				2
		#define MAPI_BCC			3
		#define IMPORTANCE_NORMAL	1
		
		local lcPath, ;
			lcRecipients, ;
			laRecipients[1], ;
			lnRecipients, ;
			lnI, ;
			laAttachments[1], ;
			lnAttachments, ;
			llReturn
		with This
		
* Open the library if necessary.
		
			if not '\VFPEXMAPI.FLL' $ upper(set('Library'))
				lcPath = ''
				if not file('VFPEXMAPI.FLL')
					lcPath = .GetAppFolder()
				endif not file('VFPEXMAPI.FLL')
				set library to (lcPath + 'VFPEXMAPI.FLL') additive
			endif not '\VFPEXMAPI.FLL' $ upper(set('Library'))
		
* Create the message.	
		
			if '<HTML' $ upper(.cBody) or '{\RTF' $ upper(.cBody)
				EMCreateMessageEx(.cSubject, .cBody, IMPORTANCE_NORMAL)
			else
				EMCreateMessage(.cSubject, .cBody, IMPORTANCE_NORMAL)
			endif '<HTML' $ upper(.cBody) ...
		
* Add recipients.
		
			lcRecipients = strtran(.cRecipients, ',', ';')
			lnRecipients = alines(laRecipients, lcRecipients, 4, ';')
			for lnI = 1 to lnRecipients
				EMAddRecipient(laRecipients[lnI], MAPI_TO)
			next lnI
		
* Add CC recipients.
		
			lcRecipients = strtran(.cCCRecipients, ',', ';')
			lnRecipients = alines(laRecipients, lcRecipients, 4, ';')
			for lnI = 1 to lnRecipients
				EMAddRecipient(laRecipients[lnI], MAPI_CC)
			next lnI
		
* Add BCC recipients.
		
			lcRecipients = strtran(.cBCCRecipients, ',', ';')
			lnRecipients = alines(laRecipients, lcRecipients, 4, ';')
			for lnI = 1 to lnRecipients
				EMAddRecipient(laRecipients[lnI], MAPI_BCC)
			next lnI
		
* Add attachments
		
			lnAttachments = alines(laAttachments, .cAttachments, 4, ',')
			for lnI = 1 to lnAttachments
				EMAddAttachment(laAttachments[lnI])
			next lnI
		endwith
		
* Send the message.
		
		llReturn = EMSend(.T.)
		return llReturn
	endproc

*==============================================================================
* Method:			SendMailSMTP
* Purpose:			Sends the email using SMTP
* Author:			Doug Hennig
* Last revision:	12/03/2020
* Parameters:		none
* Returns:			.T. if the message was sent
* Environment in:	the properties are set for emailing
*					wwDotNetBridge has been loaded or is available to be loaded
*					SMTPLibrary2.dll, BouncyCastle.Crypto.dll, MimeKit.dll, and
*						MailKit.dll have been loaded or are available to be
*						loaded
* Environment out:	the email may have been sent
*					wwDotNetBridge has been loaded if it wasn't before
*					SMTPLibrary2.dll, BouncyCastle.Crypto.dll, MimeKit.dll, and
*						MailKit.dll have been loaded
*==============================================================================
		
	protected procedure SendMailSMTP
		local loBridge, ;
			lcPath, ;
			loMail, ;
			laAttachments[1], ;
			lnAttachments, ;
			lnI, ;
			llReturn, ;
			loException as Exception
		with This
			try

* Get an instance of wwDotNetBridge and load the assembly.

				do wwDotNetBridge
				loBridge = GetwwDotNetBridge()
				lcPath   = ''
				if not file('SMTPLibrary2.dll')
					lcPath = .GetAppFolder()
				endif not file('SMTPLibrary2.dll')
				loBridge.LoadAssembly(lcPath + 'SMTPLibrary2.dll')

* If we succeeded, instantiate the SMTP class and set its properties.

				if empty(loBridge.cErrorMsg)
					loMail                 = loBridge.CreateInstance('SMTPLibrary2.SMTP')
					loMail.LogFile         = .cLogFile
					loMail.SecurityOptions = .nSecurityOptions
						&& see http://www.mimekit.net/docs/html/T_MailKit_Security_SecureSocketOptions.htm
					loMail.MailServer      = .cServer
					loMail.ServerPort      = .nSMTPPort
					loMail.SenderEmail     = .cSenderEmail
					loMail.SenderName      = .cSenderName
					loMail.Recipients      = strtran(.cRecipients,    ',', ';')
					loMail.CCRecipients    = strtran(.cCCRecipients,  ',', ';')
					loMail.BCCRecipients   = strtran(.cBCCRecipients, ',', ';')
					loMail.ReplyTo         = strtran(.cReplyTo, ',', ';')
					loMail.Subject         = .cSubject
					loMail.Message         = .cBody
					loMail.Username        = .cUser
					loMail.Password        = .cPassword
					loMail.UseHtml         = '<' $ .cBody and '>' $ .cBody
					loMail.Timeout         = .nTimeout

* Add any attachments.

					lnAttachments = alines(laAttachments, .cAttachments, ;
						5, ',')
					for lnI = 1 to lnAttachments
						loBridge.InvokeMethod(loMail, 'AddAttachment', ;
							laAttachments[lnI])
					next lnI

* Sent the email and grab the error message if it failed.

					llReturn = loMail.SendMail()
					if not llReturn
						.cErrorMessage = loMail.ErrorMessage
					endif not llReturn
				else
					.cErrorMessage = loBridge.cErrorMsg
				endif empty(loBridge.cErrorMsg)
			catch to loException
				llReturn       = .F.
				.cErrorMessage = loException.Message
			endtry
		endwith
		return llReturn
	endproc
	
*==============================================================================
* Method:			GetAppFolder
* Purpose:			Finds the application folder
* Author:			Doug Hennig
* Last revision:	11/16/2010
* Parameters:		none
* Returns:			the folder the application is running from
* Environment in:	none
* Environment out:	none
*==============================================================================
		
	protected procedure GetAppFolder
		local lcProgram, ;
			lcPath
		if version(2) = 2
			lcProgram = sys(16, 0)
			lcPath    = justpath(lcProgram)
			if atc('PROCEDURE', lcPath) > 0
				lcPath = substr(lcPath, rat(':', lcPath) - 1)
			endif atc('PROCEDURE', lcPath) > 0
		else
			lcPath = justpath(_vfp.ServerName)
		endif version(2) = 2
		return addbs(lcPath)
	endproc
enddefine
