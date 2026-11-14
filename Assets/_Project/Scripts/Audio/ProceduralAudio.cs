using UnityEngine;

namespace UzbCarParking.AudioSystem
{
    /// <summary>
    /// VAQTINCHALIK tovushlar: audio fayllar hali yo‘q, shuning uchun oddiy sintez qilingan
    /// to‘lqinlar ishlatiladi. Haqiqiy yozuvlar (dvigatel, signal, tormoz) tayyor bo‘lgach,
    /// VehicleAudio'dagi AudioClip maydonlariga biriktiriladi va bu sintez ishlatilmaydi.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 22050;

        private static AudioClip engineLoop;
        private static AudioClip horn;
        private static AudioClip tickOn;
        private static AudioClip tickOff;
        private static AudioClip starter;
        private static AudioClip impact;

        public static AudioClip EngineLoop
        {
            get
            {
                if (engineLoop == null) engineLoop = BuildEngineLoop();
                return engineLoop;
            }
        }

        public static AudioClip Horn
        {
            get
            {
                if (horn == null) horn = BuildHorn();
                return horn;
            }
        }

        public static AudioClip TickOn
        {
            get
            {
                if (tickOn == null) tickOn = BuildTick("UZB_TickOn", 1800f);
                return tickOn;
            }
        }

        public static AudioClip TickOff
        {
            get
            {
                if (tickOff == null) tickOff = BuildTick("UZB_TickOff", 1200f);
                return tickOff;
            }
        }

        public static AudioClip Starter
        {
            get
            {
                if (starter == null) starter = BuildStarter();
                return starter;
            }
        }

        public static AudioClip Impact
        {
            get
            {
                if (impact == null) impact = BuildImpact();
                return impact;
            }
        }

        // 1 soniyalik uzluksiz aylanadigan dvigatel to‘lqini: asosiy chastota 45 Hz va garmonikalari.
        private static AudioClip BuildEngineLoop()
        {
            int samples = SampleRate;
            float[] data = new float[samples];
            const float baseFrequency = 45f;
            float[] harmonicGain = { 1f, 0.6f, 0.45f, 0.25f, 0.18f, 0.1f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float value = 0f;
                for (int h = 0; h < harmonicGain.Length; h++)
                {
                    value += harmonicGain[h] * Mathf.Sin(2f * Mathf.PI * baseFrequency * (h + 1) * t + h * 0.7f);
                }

                // 15 Hz modulyatsiya — silindrlar ishini eslatuvchi "titroq".
                float modulation = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 15f * t);
                data[i] = value * modulation * 0.22f;
            }

            return Create("UZB_EngineLoop", data);
        }

        private static AudioClip BuildHorn()
        {
            int samples = SampleRate / 2;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float a = Mathf.Sin(2f * Mathf.PI * 420f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 520f * t);
                float shaped = Mathf.Clamp((a + b) * 2.5f, -1f, 1f);
                data[i] = shaped * 0.35f;
            }

            return Create("UZB_Horn", data);
        }

        private static AudioClip BuildTick(string name, float frequency)
        {
            int samples = SampleRate / 25;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-t * 140f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.6f;
            }

            return Create(name, data);
        }

        private static AudioClip BuildStarter()
        {
            int samples = (int)(SampleRate * 0.7f);
            float[] data = new float[samples];
            float phase = 0f;
            System.Random random = new System.Random(7);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float frequency = Mathf.Lerp(60f, 140f, t);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float chop = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 11f * t * 0.7f * 10f);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0) * 0.15f;
                float envelope = Mathf.Sin(Mathf.PI * t);
                data[i] = (Mathf.Sin(phase) * chop + noise) * envelope * 0.4f;
            }

            return Create("UZB_Starter", data);
        }

        private static AudioClip BuildImpact()
        {
            int samples = SampleRate / 4;
            float[] data = new float[samples];
            System.Random random = new System.Random(11);
            float lowPass = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                lowPass += (noise - lowPass) * 0.12f;
                data[i] = lowPass * Mathf.Exp(-t * 18f) * 1.6f;
            }

            return Create("UZB_Impact", data);
        }

        private static AudioClip Create(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
