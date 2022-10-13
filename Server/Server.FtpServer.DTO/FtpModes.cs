using System;
using System.Collections.Generic;
using System.Text;

namespace Server.FtpServer.DTO
{
    public enum FtpModes
    {
        None = 0,
        S, // Поток
        B, // Блок
        C, // Сжатие
        Z  // Дефлирование
    }
}
