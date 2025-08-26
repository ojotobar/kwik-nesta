using KwikNesta.Contracts.Enums;
using KwikNesta.Contracts.Models;

namespace NotificationService.Workers.Models
{
    public class AuditQuery
    {
        private int _page = 1;
        private int _size = 10;
        private int _maxSize = 50;

        private DateTime _defaultStart = DateTime.MinValue;
        private DateTime _defaultEnd = DateTime.MaxValue;

        public int? Page
        {
            get => _page;
            set => _page = value.HasValue && value.Value > 0 ?
                value.Value :
                _page;
        }
        public int? PageSize
        {
            get => _size;
            set => _size = value.HasValue && value.Value > 0 ? 
                (value.Value <= _maxSize ? value.Value : _maxSize) : _size;
        }
        public DateTime? StartTime 
        { 
            get => _defaultStart;
            set => _defaultStart = value.HasValue && value.Value < EndTime ? 
                value.Value : 
                _defaultStart; 
        }
        public DateTime? EndTime 
        { 
            get => _defaultEnd; 
            set => _defaultEnd = value.HasValue && value.Value > StartTime ? 
                value.Value : 
                _defaultEnd; 
        }
        public AuditAction? Action { get; set; }
        public AuditDomain? Domain { get; set; }
    }
}
