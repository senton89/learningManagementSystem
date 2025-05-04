namespace WpfApp1.Models
{
    public enum TimeStatus
    {
        Plenty,     // Много времени
        Soon,       // Скоро дедлайн
        Critical,   // Критически мало времени
        Overdue     // Просрочено
    }

    public enum AssignementProgressStatus
    {
        NotStarted,  // Не начато
        InProgress,  // В процессе
        AlmostDone,  // Почти готово
        Done         // Завершено
    }
}
