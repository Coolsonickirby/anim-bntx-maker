using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZstdNet;

namespace AnimBNTX
{
    class AnimationBNTX
    {
        static char[] MAGIC = { 'A', 'n', 'i', 'm', 'B', 'N', 'T', 'X'};
        static List<int> USED_RANDOM_NUMBERS = new List<int>();
        string TEMP_DIR = ".\\tmp";
        static UInt32 VERSION_MAJOR = 1;
        static UInt32 VERSION_MINOR = 1;
        public UInt32 group_number; // What group of animation frame to follow
        UInt32 frame_count; // How many frames the animation will last for
        public UInt32 loop_animation; // 0 for no, 1 for yes
        public Int32 loop_count; // how many times to loop for (-1 for inf)
        public UInt32 starting_frame_loop; // Which frame to loop back from after reaching the end
        public UInt32 ending_frame_loop; // At which frame to jump back to the starting_frame_loop?
        UInt32 relocation_table_size; // size of footer data (is not constant)
        UInt32 image_data_count; // How many images there are
        UInt32 image_data_size; // Size of each image (consistent across all images)
        List<ImageData> image_datas;
        public float frame_rate; // fastest rate at which images can switch
        List<FrameData> frameDatas; // keyframe data
        byte[] BNTX_TEMPLATE_HEADER;
        byte[] BNTX_TEMPLATE_FOOTER;
        List<byte[]> compressed_datas;

        public int max_frames;
        public int frame_multiplier;

        struct FrameData
        {
            public UInt32 keyframe_num; // at what frame to play
            public UInt32 image_index; // image to show (image_data_offset * image_index = of bntx image data)
        }

        struct ImageData
        {
            public UInt32 offset; // at what part (releative to all image data) does the compressed data start
            public UInt32 size; // how much to read
        }

        public AnimationBNTX()
        {
            Random rnd = new Random();
            int random_num = rnd.Next(0, 99999);
            while (AnimationBNTX.USED_RANDOM_NUMBERS.Contains(random_num))
            {
                random_num = rnd.Next(0, 99999);
            }
            AnimationBNTX.USED_RANDOM_NUMBERS.Add(random_num);
            this.TEMP_DIR = $".\\tmp_{random_num}";
        }

        public void LoadTemplateFromBNTX(string bntx_path)
        {
            using(FileStream reader = File.Open(bntx_path, FileMode.Open))
            {
                using(BinaryReader bin_read = new BinaryReader(reader))
                {
                    this.BNTX_TEMPLATE_HEADER = bin_read.ReadBytes(0x1000);
                    bin_read.BaseStream.Seek(0x18, SeekOrigin.Begin);
                    UInt32 relocation_table_offset = bin_read.ReadUInt32();
                    bin_read.BaseStream.Seek(relocation_table_offset, SeekOrigin.Begin);
                    this.BNTX_TEMPLATE_FOOTER = bin_read.ReadBytes((int)(bin_read.BaseStream.Length - relocation_table_offset));
                    this.relocation_table_size = (UInt32)this.BNTX_TEMPLATE_FOOTER.Length;
                    this.image_data_size = (UInt32)(bin_read.BaseStream.Length - (this.BNTX_TEMPLATE_FOOTER.Length + this.BNTX_TEMPLATE_HEADER.Length));
                    bin_read.Close();
                }
                reader.Close();
            }
        }

        private void ExtractFramesFromVideo(string video_path, bool add_border)
        {
            try { Directory.Delete(this.TEMP_DIR, true); } catch (Exception e) { }
            try { Directory.CreateDirectory(this.TEMP_DIR); } catch (Exception e) { }

            string extension = Path.GetExtension(video_path);

            string arguments = extension == ".webm" ?
                $"-y -vcodec libvpx-vp9 -i \"{video_path}\" {(add_border ? "-filter_complex \"[0]pad=w=5+iw:h=5+ih:x=2.5:y=2.5:color=black@0\"" : "")} -start_number 0 {this.TEMP_DIR}\\%04d.png"
                :
                $"-y -i \"{video_path}\" {(add_border ? "-filter_complex \"[0]pad=w=5+iw:h=5+ih:x=2.5:y=2.5:color=black\"" : "")} -start_number 0 {this.TEMP_DIR}\\%04d.png";

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"ffmpeg",
                    Arguments = arguments,
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = false,
                }
            };

            proc.Start();
            proc.WaitForExit();

            return;
        }

        private void ConvertFramesToBNTX()
        {
            List<string> files = new List<string>();
            foreach(string file in Directory.GetFiles(this.TEMP_DIR))
            {
                if (file.EndsWith(".png"))
                {
                    files.Add(file);
                }
            }

            files.Sort((a, b) => {
                int length_of_file_a = a.LastIndexOf('.') - (this.TEMP_DIR.Length + 1);
                int length_of_file_b = b.LastIndexOf('.') - (this.TEMP_DIR.Length + 1);
                return Int32.Parse(a.Substring(this.TEMP_DIR.Length + 1, length_of_file_a)).CompareTo(Int32.Parse(b.Substring(this.TEMP_DIR.Length + 1, length_of_file_b)));
            });

            for(int i = 0; i < (this.max_frames != -1 ? this.max_frames : files.Count / this.frame_multiplier); i++)
            {
                string file = files[i * this.frame_multiplier];
                string extension = Path.GetExtension(file);
                string output_path = file.Replace(extension, ".bntx");
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\\Users\\Random\\Downloads\\Release (1)\\bntx-thingy-for-csk-bc1.exe",
                        Arguments = $"\"{file}\" \"chara_7_saul_00\" \"{output_path}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        CreateNoWindow = true,
                    }
                };

                proc.Start();
                proc.WaitForExit();
            }
        }

        private List<String> GetBNTXFiles(string bntx_files_path)
        {
            List<String> files = new List<String>();
            foreach(string file in Directory.GetFiles(bntx_files_path))
            {
                if (file.EndsWith(".bntx"))
                {
                    files.Add(file);
                }
            }

            files.Sort((a, b) => {
                int length_of_file_a = a.LastIndexOf('.') - (bntx_files_path.Length + 1);
                int length_of_file_b = b.LastIndexOf('.') - (bntx_files_path.Length + 1);
                return Int32.Parse(a.Substring(bntx_files_path.Length + 1, length_of_file_a)).CompareTo(Int32.Parse(b.Substring(bntx_files_path.Length + 1, length_of_file_b)));
            });

            return files;
        }

        public void LoadFromVideo(string video_path, bool add_border)
        {
            ExtractFramesFromVideo(video_path, add_border);
            ConvertFramesToBNTX();
            List<String> bntx_files = GetBNTXFiles(this.TEMP_DIR);
            LoadTemplateFromBNTX(bntx_files[0]);
            SetupImageAndFrameDatas(bntx_files);
            try { Directory.Delete(this.TEMP_DIR, true); } catch (Exception e) { }
        }

        private byte[] ReadBNTXFileToBytes(string path)
        {
            byte[] res;
            using (FileStream reader = File.Open(path, FileMode.Open))
            {
                using (BinaryReader bin_read = new BinaryReader(reader))
                {
                    bin_read.BaseStream.Seek(0x1000, SeekOrigin.Begin);
                    res = bin_read.ReadBytes((int)this.image_data_size);
                    bin_read.Close();
                }
                reader.Close();
            }
            return res;
        }

        private void SetupImageAndFrameDatas(List<String> bntx_files)
        {
            this.frameDatas = new List<FrameData>();
            this.image_datas = new List<ImageData>();
            
            UInt32 index = 0;
            List<byte[]> image_datas = new List<byte[]>();
            for (int i = 0; i < bntx_files.Count; i++)
            {
                string file = bntx_files[i];
                byte[] data = ReadBNTXFileToBytes(file);

                UInt32 found_index = 0;
                bool found = false;
                for (UInt32 search_index = 0; search_index < image_datas.Count; search_index++)
                {
                    found = image_datas[(int)search_index].SequenceEqual(data);
                    if (found)
                    {
                        found_index = search_index;
                        break;
                    }
                }

                if (!found)
                {
                    image_datas.Add(data);
                } else
                {
                    Console.WriteLine($"Found duplicate image! Src Index: {index} - Found Index: {found_index}");
                }

                FrameData frame_data = new FrameData();
                frame_data.keyframe_num = index;
                frame_data.image_index = found ? found_index : (UInt32)image_datas.Count - 1;

                this.frameDatas.Add(frame_data);
                index++;
            }
            this.image_data_count = (UInt32)image_datas.Count;
            this.frame_count = (UInt32)this.frameDatas.Count;

            // Compressing image_datas
            CompressionOptions options = new CompressionOptions(CompressionOptions.DefaultCompressionLevel);
            UInt32 current_image_data_offset = 0;
            this.compressed_datas = new List<byte[]>();

            foreach (byte[] data in image_datas)
            {
                Compressor compressor = new Compressor(options);
                byte[] compressed_data = compressor.Wrap(data);
                ImageData image_data_info = new ImageData();
                image_data_info.size = (UInt32)compressed_data.Length;
                image_data_info.offset = current_image_data_offset;
                current_image_data_offset += (UInt32)compressed_data.Length;
                this.compressed_datas.Add(compressed_data);
                this.image_datas.Add(image_data_info);
            }
        }

        public void SaveAnimBNTX(string out_path)
        {
            if(this.ending_frame_loop == 0)
            {
                this.ending_frame_loop = this.frame_count - 1;
            }

            

            using(FileStream stream_out = File.Open(out_path, FileMode.Create)){
                using(BinaryWriter writer = new BinaryWriter(stream_out))
                {
                    writer.Write(AnimationBNTX.MAGIC);
                    writer.Write(AnimationBNTX.VERSION_MAJOR);
                    writer.Write(AnimationBNTX.VERSION_MINOR);
                    writer.Write(this.group_number); // Replicate these new additions in the rust side
                    writer.Write(this.frame_count);
                    writer.Write(this.loop_animation);
                    writer.Write(this.loop_count);
                    writer.Write(this.starting_frame_loop);
                    writer.Write(this.ending_frame_loop);
                    writer.Write(this.relocation_table_size);
                    writer.Write(this.image_data_count);
                    writer.Write(this.image_data_size);
                    foreach(ImageData image_data in this.image_datas)
                    {
                        writer.Write(image_data.offset);
                        writer.Write(image_data.size);
                    }
                    writer.Write(this.frame_rate);
                    foreach(FrameData frame in this.frameDatas) {
                        writer.Write(frame.keyframe_num);
                        writer.Write(frame.image_index);
                    }
                    writer.Write(this.BNTX_TEMPLATE_HEADER);
                    writer.Write(this.BNTX_TEMPLATE_FOOTER);
                    foreach(byte[] data in this.compressed_datas)
                    {
                        writer.Write(data);
                    }
                    writer.Close();
                }
                stream_out.Close();
            }
        }
    }

}
