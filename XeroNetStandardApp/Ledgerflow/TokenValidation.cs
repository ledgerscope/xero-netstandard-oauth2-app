namespace XeroNetStandardApp.Ledgerflow
{
	public class TokenValidation
	{
		public bool IsValid { get; }
		public string Message { get; }

		public TokenValidation(bool isValid, string message = null)
		{
			this.IsValid = isValid;
			this.Message = message;
		}
	}
}
