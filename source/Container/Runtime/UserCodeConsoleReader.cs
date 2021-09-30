using System;
using System.IO;

namespace SharpLab.Container.Runtime {
    public class UserCodeConsoleReader : TextReader {
        private const string Message = "🚧 For now, Console.Read in SharpLab is not interactive and always returns this message.";

        private int _position;

        public override int Read() {
            if (_position >= Message.Length)
                return -1;

            var @char = Message[_position];
            _position += 1;
            return @char;
        }

        public override string? ReadLine() {
            if (_position == 0) {
                _position = Message.Length;
                return Message;
            }

            if (_position == Message.Length)
                return null;

            throw new NotSupportedException();
        }

        public void Reset() {
            _position = 0;
        }
    }
}
