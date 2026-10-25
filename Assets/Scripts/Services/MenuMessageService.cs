namespace Services
{
    public class MenuMessageService
    {
        private string _pending;

        public void Set(string message) => _pending = message;

        public string Take()
        {
            var message = _pending;
            _pending = null;
            return message;
        }
    }
}
