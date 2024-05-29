using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace Pin {
    class PosHelper {
        public static async Task<bool> waitFor(Func<bool> condition, int timeout) {
            for(int i = 0; i < (timeout * 10); i++) {
                if(condition())
                    return true;
                await Task.Delay(100);
            }
            return false;
        }
    }
}
